using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DataGateMonitor.Services.HealthChecks;

namespace DataGateMonitor.Tests.Services.HealthChecks;

public class CustomServiceHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Returns_Healthy_With_Message()
    {
        // arrange
        var healthCheck = new CustomServiceHealthCheck();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                name: "custom",
                instance: healthCheck,
                failureStatus: null,
                tags: null)
        };

        // act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Service OK", result.Description);
    }
}
