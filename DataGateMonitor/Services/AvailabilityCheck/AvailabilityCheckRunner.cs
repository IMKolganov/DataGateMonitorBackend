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

    /// <summary>
    /// Enable/disable the probe for one VPN server, then refresh the status snapshot.
    /// </summary>
    Task<AvailabilityCheckStatusResponse?> SetServerEnabledAsync(
        int vpnServerId,
        bool enabled,
        CancellationToken ct);
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
    public async Task<AvailabilityCheckStatusResponse?> SetServerEnabledAsync(
        int vpnServerId,
        bool enabled,
        CancellationToken ct)
    {
        var server = await db.Set<VpnServer>()
            .FirstOrDefaultAsync(s => s.Id == vpnServerId && !s.IsDeleted, ct)
            .ConfigureAwait(false);
        if (server is null)
            return null;

        server.IsAvailabilityCheckEnabled = enabled;
        if (!enabled)
        {
            // Opting out must not leave Online blocked by a stale probe result.
            server.IsAvailableByExternalProbe = true;
            server.ExternalProbeSummary = "check disabled for this server";
            server.ExternalProbeCheckedAtUtc = DateTimeOffset.UtcNow;
            notificationTracker.ClearUnreachable($"server:{server.Id}");
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Re-run: probes only servers that still have the per-server flag on.
        return await RunAsync(ct, force: enabled).ConfigureAwait(false);
    }

    public async Task<AvailabilityCheckStatusResponse> RunAsync(CancellationToken ct, bool force = false)
    {
        var (enabled, probeUrl, intervalSeconds) = await ResolveSettingsAsync(ct).ConfigureAwait(false);

        var snapshot = new AvailabilityCheckStatusResponse
        {
            Enabled = enabled,
            ProbeUrl = probeUrl,
            IntervalSeconds = intervalSeconds,
            LastCheckedAtUtc = DateTimeOffset.UtcNow,
        };

        var servers = await db.Set<VpnServer>()
            .Where(s => !s.IsDeleted && !s.IsDisable)
            .OrderBy(s => s.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (!enabled && !force)
        {
            // Global kill-switch off → clear blocks so manager IsOnline alone drives the badge.
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

        var toProbe = servers.Where(s => s.IsAvailabilityCheckEnabled).ToList();
        logger.LogDebug(
            "Availability check: probing {Count}/{Total} server(s) via {ProbeUrl}",
            toProbe.Count,
            servers.Count,
            probeUrl);

        foreach (var server in servers)
        {
            ct.ThrowIfCancellationRequested();

            if (!server.IsAvailabilityCheckEnabled)
            {
                if (!server.IsAvailableByExternalProbe)
                {
                    server.IsAvailableByExternalProbe = true;
                    server.ExternalProbeSummary = "check disabled for this server";
                    server.ExternalProbeCheckedAtUtc = DateTimeOffset.UtcNow;
                }

                snapshot.Servers.Add(ToDtoWithoutProbe(server));
                continue;
            }

            var apiUrl = (server.ApiUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                snapshot.Servers.Add(new AvailabilityCheckServerResultDto
                {
                    VpnServerId = server.Id,
                    ServerName = server.ServerName,
                    ApiUrl = apiUrl,
                    IsAvailabilityCheckEnabled = true,
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
                    IsAvailabilityCheckEnabled = true,
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
                IsAvailabilityCheckEnabled = true,
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

    private async Task<(bool Enabled, string ProbeUrl, int IntervalSeconds)> ResolveSettingsAsync(CancellationToken ct)
    {
        var storedProbe = await settingsService
            .GetValueAsync<string>(AvailabilityCheckSettingsKeys.ProbeUrl, ct)
            .ConfigureAwait(false);

        var probeUrl = string.IsNullOrWhiteSpace(storedProbe)
            ? AvailabilityCheckSettingsKeys.DefaultProbeUrl
            : storedProbe.Trim();

        var storedInterval = await settingsService
            .GetValueAsync<int>(AvailabilityCheckSettingsKeys.IntervalSeconds, ct)
            .ConfigureAwait(false);
        var intervalSeconds = storedInterval > 0
            ? AvailabilityCheckSettingsKeys.ClampIntervalSeconds(storedInterval)
            : AvailabilityCheckSettingsKeys.DefaultIntervalSeconds;

        var storedEnabled = await settingsService
            .GetValueAsync<bool>(AvailabilityCheckSettingsKeys.Enabled, ct)
            .ConfigureAwait(false);

        if (storedProbe is null)
            return (AvailabilityCheckSettingsKeys.DefaultEnabled, probeUrl, intervalSeconds);

        return (storedEnabled, probeUrl, intervalSeconds);
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
        IsAvailabilityCheckEnabled = server.IsAvailabilityCheckEnabled,
        IsAvailableByExternalProbe = server.IsAvailableByExternalProbe,
        Summary = server.IsAvailabilityCheckEnabled
            ? server.ExternalProbeSummary
            : (server.ExternalProbeSummary ?? "check disabled for this server"),
        CheckedAtUtc = server.ExternalProbeCheckedAtUtc,
    };

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}
