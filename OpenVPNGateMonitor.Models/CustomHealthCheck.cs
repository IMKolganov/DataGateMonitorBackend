using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OpenVPNGateMonitor.Models;

public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = true;
        return Task.FromResult(
            isHealthy
                ? HealthCheckResult.Healthy("OK")
                : HealthCheckResult.Unhealthy("Something went wrong"));
    }
}