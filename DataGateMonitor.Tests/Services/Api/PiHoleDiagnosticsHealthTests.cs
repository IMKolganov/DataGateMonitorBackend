using DataGateMonitor.Services.Api;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

namespace DataGateMonitor.Tests.Services.Api;

public class PiHoleDiagnosticsHealthTests
{
    [Fact]
    public void Apply_MarksDisabled_WhenServerFlagOff()
    {
        var dto = new PiHoleDiagnosticsResponse { Enabled = true, Authenticated = true };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: false);
        Assert.Equal("Disabled", dto.Health);
    }

    [Fact]
    public void Apply_MarksError_WhenProbeFailed()
    {
        var dto = new PiHoleDiagnosticsResponse { Enabled = true, Error = "Connection refused" };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: true);
        Assert.Equal("Error", dto.Health);
        Assert.Contains("Connection refused", dto.HealthMessage);
    }

    [Fact]
    public void Apply_MarksOk_WhenHealthyAndDataFlowing()
    {
        var dto = new PiHoleDiagnosticsResponse
        {
            Enabled = true,
            Authenticated = true,
            CollectorRunning = true,
            PollIntervalSeconds = 60,
            LastSuccessfulPollAtUtc = DateTime.UtcNow.AddSeconds(-30),
            LastPollQueriesForwarded = 5,
            StoredQueryCount = 100
        };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: true);
        Assert.Equal("Ok", dto.Health);
    }
}
