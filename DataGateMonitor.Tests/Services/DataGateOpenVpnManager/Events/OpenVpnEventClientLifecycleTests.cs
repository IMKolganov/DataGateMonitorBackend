using DataGateMonitor.Hubs;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClientLifecycleTests
{
    private static OpenVpnEventClient CreateClient()
    {
        var services = new ServiceCollection();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(Mock.Of<IServiceScope>());

        return new OpenVpnEventClient(
            OpenVpnHubTestHelpers.OpenVpnServer(),
            NullLogger<OpenVpnEventClient>.Instance,
            Mock.Of<IHubContext<OpenVpnEventHub>>(),
            OpenVpnHubTestHelpers.CreateTokenService(),
            scopeFactory.Object);
    }

    [Fact]
    public void GetStatus_BeforeStart_ReportsDisconnected()
    {
        var client = CreateClient();
        var status = client.GetStatus();

        Assert.Equal("Disconnected", status.ConnectionStatus.State);
        Assert.Equal(75, status.ConnectionStatus.ServerId);
    }

    [Fact]
    public async Task StopAsync_WhenNeverStarted_DoesNotThrow()
    {
        var client = CreateClient();
        await client.StopAsync();
    }

    [Fact]
    public async Task StopAsync_AfterStop_RemainsDisconnected()
    {
        var client = CreateClient();
        await client.StopAsync();

        Assert.Equal("Disconnected", client.GetStatus().ConnectionStatus.State);
    }
}
