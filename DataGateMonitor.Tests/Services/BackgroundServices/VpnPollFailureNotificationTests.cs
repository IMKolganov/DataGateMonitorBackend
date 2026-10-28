using DataGateMonitor.Services.BackgroundServices;

namespace DataGateMonitor.Tests.Services.BackgroundServices;

public class VpnPollFailureNotificationTests
{
    [Fact]
    public void Step_IsReadBack_EvenWhenWrapped()
    {
        var inner = new HttpRequestException("reset");
        VpnPollStep.Tag(inner, "connected clients");
        var wrapped = new InvalidOperationException("outer", inner);

        Assert.Equal("connected clients", VpnPollStep.Get(wrapped));
    }

    [Fact]
    public void Step_FirstTagWins()
    {
        var ex = new Exception("x");
        VpnPollStep.Tag(ex, "status log");
        VpnPollStep.Tag(ex, "mark online");

        Assert.Equal("status log", VpnPollStep.Get(ex));
    }

    [Fact]
    public void Step_IsNullWhenUntagged()
    {
        Assert.Null(VpnPollStep.Get(new Exception("x")));
    }

    [Fact]
    public void Details_IncludeAttemptStepUrlAndError()
    {
        var text = OpenVpnBackgroundService.BuildFailureNotificationDetails(
            3, "connected clients", "https://node:5001", "TLS handshake failed: Connection reset by peer");

        Assert.Equal(
            "Attempt=3; Step=connected clients; Url=https://node:5001; TLS handshake failed: Connection reset by peer",
            text);
    }

    [Fact]
    public void Details_SkipMissingStepAndUrl()
    {
        Assert.Equal("Attempt=1; boom",
            OpenVpnBackgroundService.BuildFailureNotificationDetails(1, null, null, "boom"));
    }
}
