using System.Data;
using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Session-scoped advisory lock. Keeps one DbContext/connection open for the hold lifetime
/// so other instances' <c>pg_try_advisory_lock</c> fails until release.
/// </summary>
public sealed class PostgresTrafficRewriteAdvisoryLock(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    : ITrafficRewriteAdvisoryLock, IAsyncDisposable
{
    /// <summary>ASCII 'TFWR' — traffic rewrite.</summary>
    public const long LockKey = 0x54465752L;

    private ApplicationDbContext? _ctx;
    private bool _held;

    public async Task<bool> TryAcquireAsync(CancellationToken ct)
    {
        if (_held)
            return true;

        _ctx = await dbContextFactory.CreateDbContextAsync(ct);
        if (!IsNpgsql(_ctx))
        {
            _held = true;
            return true;
        }

        var conn = _ctx.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT pg_try_advisory_lock({LockKey})";
        var scalar = await cmd.ExecuteScalarAsync(ct);
        var acquired = scalar is true || scalar is bool b && b;

        if (!acquired)
        {
            await _ctx.DisposeAsync();
            _ctx = null;
            return false;
        }

        _held = true;
        return true;
    }

    public async Task ReleaseAsync(CancellationToken ct)
    {
        if (!_held || _ctx is null)
            return;

        try
        {
            if (IsNpgsql(_ctx))
            {
                var conn = _ctx.Database.GetDbConnection();
                if (conn.State == ConnectionState.Open)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT pg_advisory_unlock({LockKey})";
                    await cmd.ExecuteScalarAsync(ct);
                }
            }
        }
        finally
        {
            _held = false;
            await _ctx.DisposeAsync();
            _ctx = null;
        }
    }

    public async ValueTask DisposeAsync() => await ReleaseAsync(CancellationToken.None);

    private static bool IsNpgsql(ApplicationDbContext ctx) =>
        ctx.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
