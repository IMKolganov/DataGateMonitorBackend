using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

namespace DataGateMonitor.Services.Api;

internal static class PiHoleDiagnosticsHealth
{
    public static void Apply(PiHoleDiagnosticsResponse diagnostics, bool serverPiHoleEnabled)
    {
        if (!serverPiHoleEnabled || !diagnostics.Enabled)
        {
            diagnostics.Health = "Disabled";
            diagnostics.HealthMessage = serverPiHoleEnabled
                ? "Pi-hole collector is disabled on the VPN microservice."
                : "Pi-hole integration is disabled for this server.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(diagnostics.Error))
        {
            diagnostics.Health = "Error";
            diagnostics.HealthMessage = diagnostics.Error;
            return;
        }

        if (!string.IsNullOrWhiteSpace(diagnostics.LastPollError))
        {
            diagnostics.Health = "Error";
            diagnostics.HealthMessage = $"Last poll failed: {diagnostics.LastPollError}";
            return;
        }

        if (!diagnostics.Authenticated)
        {
            diagnostics.Health = "Error";
            diagnostics.HealthMessage = "Pi-hole API authentication failed. Check the application password.";
            return;
        }

        if (!diagnostics.CollectorRunning)
        {
            diagnostics.Health = "Warning";
            diagnostics.HealthMessage = "Pi-hole collector is not running on the microservice (check container logs and BaseUrl).";
            return;
        }

        var staleThresholdSec = Math.Max(120, diagnostics.PollIntervalSeconds * 3);
        if (diagnostics.LastSuccessfulPollAtUtc is null)
        {
            diagnostics.Health = "Warning";
            diagnostics.HealthMessage = "Collector is running but no successful poll has been recorded yet.";
            return;
        }

        var ageSec = (DateTime.UtcNow - diagnostics.LastSuccessfulPollAtUtc.Value).TotalSeconds;
        if (ageSec > staleThresholdSec)
        {
            diagnostics.Health = "Warning";
            diagnostics.HealthMessage =
                $"No successful Pi-hole poll in {Math.Round(ageSec)}s (threshold {staleThresholdSec}s).";
            return;
        }

        if (diagnostics.StoredQueryCount == 0 && diagnostics.LastPollQueriesForwarded == 0)
        {
            diagnostics.Health = "Warning";
            diagnostics.HealthMessage =
                "Pi-hole connection works but no VPN DNS queries were forwarded or stored yet.";
            return;
        }

        diagnostics.Health = "Ok";
        diagnostics.HealthMessage = diagnostics.LastPollQueriesForwarded > 0
            ? $"Last poll forwarded {diagnostics.LastPollQueriesForwarded} queries; {diagnostics.StoredQueryCount} stored in DB."
            : $"Pi-hole reachable; {diagnostics.StoredQueryCount} queries stored in DB.";
    }
}
