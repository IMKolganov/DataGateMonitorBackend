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

    [Fact]
    public void Apply_MarksError_WhenLastPollFailed()
    {
        var dto = new PiHoleDiagnosticsResponse
        {
            Enabled = true,
            Authenticated = true,
            LastPollError = "timeout"
        };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: true);
        Assert.Equal("Error", dto.Health);
        Assert.Contains("timeout", dto.HealthMessage);
    }

    [Fact]
    public void Apply_MarksError_WhenNotAuthenticated()
    {
        var dto = new PiHoleDiagnosticsResponse { Enabled = true, Authenticated = false };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: true);
        Assert.Equal("Error", dto.Health);
        Assert.Contains("authentication", dto.HealthMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_MarksWarning_WhenCollectorNotRunning()
    {
        var dto = new PiHoleDiagnosticsResponse
        {
            Enabled = true,
            Authenticated = true,
            CollectorRunning = false,
            PollIntervalSeconds = 60,
            LastSuccessfulPollAtUtc = DateTime.UtcNow
        };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: true);
        Assert.Equal("Warning", dto.Health);
    }

    [Fact]
    public void Apply_MarksWarning_WhenPollStale()
    {
        var dto = new PiHoleDiagnosticsResponse
        {
            Enabled = true,
            Authenticated = true,
            CollectorRunning = true,
            PollIntervalSeconds = 60,
            LastSuccessfulPollAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: true);
        Assert.Equal("Warning", dto.Health);
        Assert.Contains("No successful Pi-hole poll", dto.HealthMessage);
    }

    [Fact]
    public void Apply_MarksWarning_WhenNoDataYet()
    {
        var dto = new PiHoleDiagnosticsResponse
        {
            Enabled = true,
            Authenticated = true,
            CollectorRunning = true,
            PollIntervalSeconds = 60,
            LastSuccessfulPollAtUtc = DateTime.UtcNow.AddSeconds(-20),
            StoredQueryCount = 0,
            LastPollQueriesForwarded = 0
        };
        PiHoleDiagnosticsHealth.Apply(dto, serverPiHoleEnabled: true);
        Assert.Equal("Warning", dto.Health);
    }
}
