using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.AvailabilityCheck;

public interface IAvailabilityCheckRunner
{
    Task<AvailabilityCheckStatusResponse> RunAsync(CancellationToken ct, bool force = false);
}

public sealed class AvailabilityCheckRunner(
    ILogger<AvailabilityCheckRunner> logger,
    ISettingsService settingsService,
    IAvailabilityCheckProbeClient probeClient,
    IAvailabilityCheckStatusStore statusStore,
    IAvailabilityCheckNotificationService notificationService,
    AvailabilityCheckNotificationTracker notificationTracker,
    ApplicationDbContext db) : IAvailabilityCheckRunner
{
    public async Task<AvailabilityCheckStatusResponse> RunAsync(CancellationToken ct, bool force = false)
    {
        var (enabled, probeUrl) = await ResolveSettingsAsync(ct).ConfigureAwait(false);

        var snapshot = new AvailabilityCheckStatusResponse
        {
            Enabled = enabled,
            ProbeUrl = probeUrl,
            LastCheckedAtUtc = DateTimeOffset.UtcNow,
        };

        var servers = await db.Set<VpnServer>()
            .Where(s => !s.IsDeleted && !s.IsDisable)
            .OrderBy(s => s.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (!enabled && !force)
        {
            // Feature off → clear blocks so manager IsOnline alone drives the badge.
            foreach (var server in servers.Where(s => !s.IsAvailableByExternalProbe))
            {
                server.IsAvailableByExternalProbe = true;
                server.ExternalProbeSummary = "availability check disabled";
                server.ExternalProbeCheckedAtUtc = DateTimeOffset.UtcNow;
            }

            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

            snapshot.Servers = servers.Select(ToDtoWithoutProbe).ToList();
            statusStore.Save(snapshot);
            return snapshot;
        }

        logger.LogDebug(
            "Availability check: probing {Count} server(s) via {ProbeUrl}",
            servers.Count,
            probeUrl);

        foreach (var server in servers)
        {
            ct.ThrowIfCancellationRequested();

            var apiUrl = (server.ApiUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                snapshot.Servers.Add(new AvailabilityCheckServerResultDto
                {
                    VpnServerId = server.Id,
                    ServerName = server.ServerName,
                    ApiUrl = apiUrl,
                    IsAvailableByExternalProbe = server.IsAvailableByExternalProbe,
                    Summary = "no ApiUrl configured — skipped",
                    CheckedAtUtc = DateTimeOffset.UtcNow,
                });
                continue;
            }

            var checkedAt = DateTimeOffset.UtcNow;
            var (result, error, durationMs) = await probeClient
                .ProbeAsync(probeUrl, apiUrl, ct)
                .ConfigureAwait(false);

            // Probe transport failure: keep previous flag, record error only.
            if (result is null)
            {
                server.ExternalProbeCheckedAtUtc = checkedAt;
                server.ExternalProbeSummary = Truncate(error ?? "probe failed", 500);
                snapshot.Servers.Add(new AvailabilityCheckServerResultDto
                {
                    VpnServerId = server.Id,
                    ServerName = server.ServerName,
                    ApiUrl = apiUrl,
                    IsAvailableByExternalProbe = server.IsAvailableByExternalProbe,
                    Reachable = null,
                    Summary = server.ExternalProbeSummary,
                    CheckedAtUtc = checkedAt,
                    DurationMs = durationMs,
                    Error = error,
                });
                continue;
            }

            var reachable = result.Reachable;
            server.IsAvailableByExternalProbe = reachable;
            server.ExternalProbeCheckedAtUtc = checkedAt;
            server.ExternalProbeSummary = Truncate(
                result.Summary ?? (reachable ? "reachable" : "unreachable"),
                500);

            snapshot.Servers.Add(new AvailabilityCheckServerResultDto
            {
                VpnServerId = server.Id,
                ServerName = server.ServerName,
                ApiUrl = apiUrl,
                IsAvailableByExternalProbe = reachable,
                Reachable = reachable,
                Summary = server.ExternalProbeSummary,
                CheckedAtUtc = checkedAt,
                DurationMs = durationMs,
                Result = result,
            });

            await NotifyIfNeededAsync(server, reachable, server.ExternalProbeSummary ?? "", ct)
                .ConfigureAwait(false);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        statusStore.Save(snapshot);
        return snapshot;
    }

    private async Task<(bool Enabled, string ProbeUrl)> ResolveSettingsAsync(CancellationToken ct)
    {
        var storedProbe = await settingsService
            .GetValueAsync<string>(AvailabilityCheckSettingsKeys.ProbeUrl, ct)
            .ConfigureAwait(false);

        var probeUrl = string.IsNullOrWhiteSpace(storedProbe)
            ? AvailabilityCheckSettingsKeys.DefaultProbeUrl
            : storedProbe.Trim();

        // Enabled key missing → default true. bool GetValueAsync returns false for missing,
        // so treat "ProbeUrl never set" as never configured only when Enabled also never saved:
        // use ProbeUrl presence OR Enabled string companion is hard; instead read Enabled and
        // if ProbeUrl is null AND we never stored Enabled, default true.
        var storedEnabled = await settingsService
            .GetValueAsync<bool>(AvailabilityCheckSettingsKeys.Enabled, ct)
            .ConfigureAwait(false);

        // If neither setting exists yet, Enabled defaults to true.
        // We detect "never configured" when ProbeUrl is null — first run before any Save.
        if (storedProbe is null)
            return (AvailabilityCheckSettingsKeys.DefaultEnabled, probeUrl);

        return (storedEnabled, probeUrl);
    }

    private async Task NotifyIfNeededAsync(
        VpnServer server,
        bool reachable,
        string summary,
        CancellationToken ct)
    {
        var key = $"server:{server.Id}";
        if (reachable)
        {
            if (notificationTracker.HasUnreachableNotification(key)
                && notificationTracker.TryMarkRecoveredNotified(key))
            {
                await notificationService
                    .NotifyRecoveredAsync($"{server.ServerName} ({server.ApiUrl})", summary, ct)
                    .ConfigureAwait(false);
            }

            notificationTracker.ClearUnreachable(key);
            return;
        }

        // Dedupe by server id only (not summary) to avoid alert spam when error text changes.
        if (!notificationTracker.TryMarkUnreachableNotified(key, "unreachable"))
            return;

        await notificationService
            .NotifyUnreachableAsync($"{server.ServerName} ({server.ApiUrl})", summary, ct)
            .ConfigureAwait(false);
    }

    private static AvailabilityCheckServerResultDto ToDtoWithoutProbe(VpnServer server) => new()
    {
        VpnServerId = server.Id,
        ServerName = server.ServerName,
        ApiUrl = server.ApiUrl,
        IsAvailableByExternalProbe = server.IsAvailableByExternalProbe,
        Summary = server.ExternalProbeSummary,
        CheckedAtUtc = server.ExternalProbeCheckedAtUtc,
    };

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}
