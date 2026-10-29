using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Rewrites VpnServerClientTraffics.ExternalId in MeasuredAt week windows, then merges orphan
/// daily rows still keyed by OldExternalId. Point 3 (UPDATE on compressed Timescale chunks) is deferred.
/// </summary>
public sealed class TrafficRewriteRunner(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ITrafficRewriteAdvisoryLock advisoryLock,
    IOptions<TrafficRewriteOptions> options,
    ILogger<TrafficRewriteRunner> logger) : ITrafficRewriteRunner
{
    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        if (!await advisoryLock.TryAcquireAsync(ct))
        {
            logger.LogDebug("Traffic rewrite skipped — advisory lock held by another instance.");
            return;
        }

        try
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync(ct);
            var job = await ctx.PendingTrafficRewrites
                .Where(j => j.Status == PendingTrafficRewriteStatus.Pending
                            || j.Status == PendingTrafficRewriteStatus.Running)
                .OrderBy(j => j.Id)
                .FirstOrDefaultAsync(ct);

            if (job is null)
                return;

            await ProcessJobAsync(ctx, job, ct);
        }
        finally
        {
            await advisoryLock.ReleaseAsync(ct);
        }
    }

    private async Task ProcessJobAsync(ApplicationDbContext ctx, PendingTrafficRewrite job, CancellationToken ct)
    {
        var opts = options.Value;
        var now = DateTimeOffset.UtcNow;

        if (job.Status == PendingTrafficRewriteStatus.Pending)
        {
            job.Status = PendingTrafficRewriteStatus.Running;
            job.StartedAt ??= now;
            job.LastError = null;
            await ctx.SaveChangesAsync(ct);
        }

        try
        {
            var batches = 0;
            while (batches < opts.MaxBatchesPerJobPass)
            {
                ct.ThrowIfCancellationRequested();

                var more = await ProcessOneWeekBatchAsync(ctx, job, opts, ct);
                batches++;
                if (!more)
                {
                    await FinalizeDailiesAsync(ctx, job, ct);
                    job.Status = PendingTrafficRewriteStatus.Completed;
                    job.CompletedAt = DateTimeOffset.UtcNow;
                    job.LastError = null;
                    await ctx.SaveChangesAsync(ct);
                    logger.LogInformation(
                        "Traffic rewrite job {JobId} completed: {Rows} raw row(s) {Old} → {New}.",
                        job.Id, job.RowsUpdated, job.OldExternalId, job.NewExternalId);
                    return;
                }
            }

            await ctx.SaveChangesAsync(ct);
            logger.LogInformation(
                "Traffic rewrite job {JobId} paused after {Batches} batch(es); cursor={Cursor}.",
                job.Id, batches, job.NextRangeEndExclusive);
        }
        catch (OperationCanceledException)
        {
            await ctx.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Traffic rewrite job {JobId} failed.", job.Id);
            job.Status = PendingTrafficRewriteStatus.Failed;
            job.LastError = Truncate(ex.Message, 2048);
            await ctx.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Updates one MeasuredAt week window for OldExternalId.
    /// Returns true when more older windows may remain.
    /// </summary>
    private async Task<bool> ProcessOneWeekBatchAsync(
        ApplicationDbContext ctx,
        PendingTrafficRewrite job,
        TrafficRewriteOptions opts,
        CancellationToken ct)
    {
        if (job.NextRangeEndExclusive is null)
        {
            var maxMeasured = await ResolveMaxMeasuredAtAsync(ctx, job.OldExternalId, ct);
            if (maxMeasured is null)
            {
                // No raw rows left under Old — still run daily finalize.
                return false;
            }

            // Exclusive end just past the newest sample, aligned so the first window covers it.
            job.NextRangeEndExclusive = AlignWeekEndExclusive(maxMeasured.Value, opts.BatchWindow);
        }

        var rangeEnd = job.NextRangeEndExclusive.Value;
        var rangeStart = rangeEnd - opts.BatchWindow;

        // Plain UPDATE (stock PG / uncompressed). Timescale compressed-chunk path = point 3, after PoC measure.
        var updated = await RewriteBatchAsync(ctx, job, rangeStart, rangeEnd, ct);

        job.RowsUpdated += updated;
        job.NextRangeEndExclusive = rangeStart;

        await ctx.SaveChangesAsync(ct);

        if (updated > 0)
            logger.LogDebug(
                "Traffic rewrite job {JobId}: updated {Rows} row(s) in [{Start}, {End}).",
                job.Id, updated, rangeStart, rangeEnd);

        await VacuumTrafficsOutsideTransactionAsync(ctx, opts, ct);

        var olderExists = await OlderRowsExistAsync(ctx, job.OldExternalId, rangeStart, ct);
        return olderExists;
    }

    private static async Task<int> RewriteBatchAsync(
        ApplicationDbContext ctx,
        PendingTrafficRewrite job,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        CancellationToken ct)
    {
        if (IsSqlite(ctx))
        {
            // SQLite cannot translate DateTimeOffset range filters in ExecuteUpdate.
            var rows = await ctx.VpnServerClientTraffics
                .Where(t => t.ExternalId == job.OldExternalId)
                .ToListAsync(ct);
            var now = DateTimeOffset.UtcNow;
            var matched = 0;
            foreach (var row in rows)
            {
                if (row.MeasuredAt < rangeStart || row.MeasuredAt >= rangeEnd)
                    continue;
                row.ExternalId = job.NewExternalId;
                row.LastUpdate = now;
                matched++;
            }

            return matched;
        }

        return await ctx.VpnServerClientTraffics
            .Where(t => t.ExternalId == job.OldExternalId
                        && t.MeasuredAt >= rangeStart
                        && t.MeasuredAt < rangeEnd)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.ExternalId, job.NewExternalId)
                    .SetProperty(t => t.LastUpdate, DateTimeOffset.UtcNow),
                ct);
    }

    private static async Task<bool> OlderRowsExistAsync(
        ApplicationDbContext ctx,
        string oldExternalId,
        DateTimeOffset before,
        CancellationToken ct)
    {
        if (IsSqlite(ctx))
        {
            var remaining = await ctx.VpnServerClientTraffics
                .Where(t => t.ExternalId == oldExternalId)
                .Select(t => t.MeasuredAt)
                .ToListAsync(ct);
            return remaining.Any(m => m < before);
        }

        return await ctx.VpnServerClientTraffics
            .AnyAsync(t => t.ExternalId == oldExternalId && t.MeasuredAt < before, ct);
    }

    private static bool IsSqlite(ApplicationDbContext ctx) =>
        ctx.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Merge-or-rekey daily rows still on OldExternalId (rollup may have written orphans during the job).
    /// Conflict key: (VpnServerId, SessionId, DayUtc).
    /// </summary>
    internal static async Task FinalizeDailiesAsync(
        ApplicationDbContext ctx,
        PendingTrafficRewrite job,
        CancellationToken ct)
    {
        var oldRows = await ctx.VpnServerClientTrafficDailies
            .Where(d => d.ExternalId == job.OldExternalId)
            .ToListAsync(ct);

        if (oldRows.Count == 0)
            return;

        foreach (var old in oldRows)
        {
            var existing = await ctx.VpnServerClientTrafficDailies
                .FirstOrDefaultAsync(
                    d => d.ExternalId == job.NewExternalId
                         && d.VpnServerId == old.VpnServerId
                         && d.SessionId == old.SessionId
                         && d.DayUtc == old.DayUtc,
                    ct);

            if (existing is null)
            {
                old.ExternalId = job.NewExternalId;
                continue;
            }

            existing.TrafficInBytes += old.TrafficInBytes;
            existing.TrafficOutBytes += old.TrafficOutBytes;
            existing.SampleCount += old.SampleCount;
            existing.LastUpdate = DateTimeOffset.UtcNow;
            ctx.VpnServerClientTrafficDailies.Remove(old);
        }

        await ctx.SaveChangesAsync(ct);
    }

    private async Task VacuumTrafficsOutsideTransactionAsync(
        ApplicationDbContext ctx,
        TrafficRewriteOptions opts,
        CancellationToken ct)
    {
        if (!opts.VacuumAfterBatch)
            return;

        if (ctx.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
            return;

        // VACUUM cannot run inside a transaction — must be a separate connection/command.
        // Schema comes from EF model config, not user input.
        var schema = (ctx.Model.GetDefaultSchema() ?? "public").Replace("\"", "\"\"", StringComparison.Ordinal);
        var sql = $"VACUUM (ANALYZE) \"{schema}\".\"VpnServerClientTraffics\"";
        await using var vacuumCtx = await dbContextFactory.CreateDbContextAsync(ct);
#pragma warning disable EF1002 // Identifier list is model-derived; VACUUM cannot take parameters.
        await vacuumCtx.Database.ExecuteSqlRawAsync(sql, ct);
#pragma warning restore EF1002
    }

    private static async Task<DateTimeOffset?> ResolveMaxMeasuredAtAsync(
        ApplicationDbContext ctx,
        string oldExternalId,
        CancellationToken ct)
    {
        var query = ctx.VpnServerClientTraffics.Where(t => t.ExternalId == oldExternalId);

        // SQLite cannot Max/OrderBy DateTimeOffset; production is PostgreSQL.
        if (IsSqlite(ctx))
        {
            var values = await query.Select(t => t.MeasuredAt).ToListAsync(ct);
            return values.Count == 0 ? null : values.Max();
        }

        return await query.MaxAsync(t => (DateTimeOffset?)t.MeasuredAt, ct);
    }

    private static DateTimeOffset AlignWeekEndExclusive(DateTimeOffset maxMeasured, TimeSpan window)
    {
        // Small epsilon so maxMeasured falls into [end - window, end).
        var end = maxMeasured.AddTicks(1);
        var ticks = window.Ticks;
        if (ticks <= 0)
            return end;

        var aligned = ((end.UtcTicks + ticks - 1) / ticks) * ticks;
        return new DateTimeOffset(aligned, TimeSpan.Zero);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
