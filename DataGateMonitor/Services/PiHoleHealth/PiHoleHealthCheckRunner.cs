using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Interfaces;

namespace DataGateMonitor.Services.PiHoleHealth;

public sealed class PiHoleHealthCheckRunner(
    ILogger<PiHoleHealthCheckRunner> logger,
    IVpnServerQueryService vpnServerQueryService,
    IVpnServerPiHoleConfigService piHoleConfigService,
    IPiHoleHealthNotificationService notificationService,
    PiHoleHealthNotificationTracker notificationTracker) : IPiHoleHealthCheckRunner
{
    public async Task RunAsync(CancellationToken ct)
    {
        var servers = await vpnServerQueryService.GetAll(ct: ct).ConfigureAwait(false);
        var piHoleServers = servers
            .Where(s => s.IsPiHoleEnabled && !s.IsDeleted && !s.IsDisable)
            .ToList();

        if (piHoleServers.Count == 0)
            return;

        logger.LogDebug("Pi-hole health check: evaluating {Count} server(s)", piHoleServers.Count);

        foreach (var server in piHoleServers)
        {
            ct.ThrowIfCancellationRequested();
            await EvaluateServerAsync(server, ct).ConfigureAwait(false);
        }
    }

    private async Task EvaluateServerAsync(VpnServer server, CancellationToken ct)
    {
        try
        {
            var diagnostics = await piHoleConfigService
                .GetMicroserviceDiagnosticsAsync(server.Id, ct)
                .ConfigureAwait(false);

            if (IsHealthy(diagnostics.Health))
            {
                if (notificationTracker.HasUnhealthyNotification(server.Id)
                    && notificationTracker.TryMarkRecoveredNotified(server.Id))
                {
                    logger.LogInformation(
                        "Pi-hole recovered on VpnServerId={VpnServerId} ({ServerName})",
                        server.Id,
                        server.ServerName);

                    await notificationService.NotifyRecoveredAsync(
                        server.Id,
                        server.ServerName,
                        ct).ConfigureAwait(false);
                }

                notificationTracker.ClearUnhealthy(server.Id);
                return;
            }

            if (!notificationTracker.TryMarkUnhealthyNotified(
                    server.Id,
                    diagnostics.Health ?? "Unknown",
                    diagnostics.HealthMessage ?? diagnostics.Error ?? "Unknown issue"))
            {
                return;
            }

            logger.LogWarning(
                "Pi-hole unhealthy on VpnServerId={VpnServerId} ({ServerName}): {Health} — {Message}",
                server.Id,
                server.ServerName,
                diagnostics.Health,
                diagnostics.HealthMessage);

            await notificationService.NotifyUnhealthyAsync(
                server.Id,
                server.ServerName,
                diagnostics.Health ?? "Unknown",
                diagnostics.HealthMessage ?? diagnostics.Error ?? "Unknown issue",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!notificationTracker.TryMarkUnhealthyNotified(server.Id, "Error", ex.Message))
                return;

            logger.LogError(
                ex,
                "Pi-hole health check failed for VpnServerId={VpnServerId} ({ServerName})",
                server.Id,
                server.ServerName);

            await notificationService.NotifyUnhealthyAsync(
                server.Id,
                server.ServerName,
                "Error",
                ex.Message,
                ct).ConfigureAwait(false);
        }
    }

    private static bool IsHealthy(string? health) =>
        string.Equals(health, "Ok", StringComparison.OrdinalIgnoreCase);
}
