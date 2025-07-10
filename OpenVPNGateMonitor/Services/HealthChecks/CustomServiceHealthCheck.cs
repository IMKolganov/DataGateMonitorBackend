using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OpenVPNGateMonitor.Services.HealthChecks;

public class CustomServiceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var success = true;
        return Task.FromResult(success 
            ? HealthCheckResult.Healthy("Service OK") 
            : HealthCheckResult.Unhealthy("Service down"));
    }
}
