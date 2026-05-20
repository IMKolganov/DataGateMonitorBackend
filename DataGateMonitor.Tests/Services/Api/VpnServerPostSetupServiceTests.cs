using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.Services.Api.PostSetup;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api;

public class VpnServerPostSetupServiceTests
{
    private static VpnServerPostSetupService CreateService(
        Mock<IVpnDataService> vpnData,
        out ServiceProvider provider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(vpnData.Object);
        services.AddSingleton<ILogger<VpnServerPostSetupService>>(_ => Mock.Of<ILogger<VpnServerPostSetupService>>());
        provider = services.BuildServiceProvider();
        return new VpnServerPostSetupService(provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<ILogger<VpnServerPostSetupService>>());
    }

    [Fact]
    public async Task StartAsync_ReturnsQueuedClone_WithUniqueOperationId()
    {
        var vpnData = new Mock<IVpnDataService>(MockBehavior.Strict);
        var svc = CreateService(vpnData, out var provider);
        using (provider)
        {
            var a = await svc.StartAsync(10, CancellationToken.None);
            var b = await svc.StartAsync(10, CancellationToken.None);

            Assert.Equal(VpnServerPostSetupState.Queued, a.State);
            Assert.Equal(10, a.VpnServerId);
            Assert.False(string.IsNullOrWhiteSpace(a.OperationId));
            Assert.NotEqual(a.OperationId, b.OperationId);
            Assert.Equal("queued", a.CurrentStep);
        }
    }

    [Fact]
    public async Task GetStatusAsync_ByOperationId_ReturnsNull_WhenServerIdMismatch()
    {
        var vpnData = new Mock<IVpnDataService>(MockBehavior.Strict);
        var svc = CreateService(vpnData, out var provider);
        using (provider)
        {
            var started = await svc.StartAsync(20, CancellationToken.None);

            var status = await svc.GetStatusAsync(21, started.OperationId, CancellationToken.None);

            Assert.Null(status);
        }
    }

    [Fact]
    public async Task GetStatusAsync_WithoutOperationId_ReturnsLatestForServer()
    {
        var vpnData = new Mock<IVpnDataService>(MockBehavior.Strict);
        var svc = CreateService(vpnData, out var provider);
        using (provider)
        {
            await svc.StartAsync(30, CancellationToken.None);
            var second = await svc.StartAsync(30, CancellationToken.None);

            var latest = await svc.GetStatusAsync(30, null, CancellationToken.None);

            Assert.NotNull(latest);
            Assert.Equal(second.OperationId, latest!.OperationId);
        }
    }

    [Fact]
    public async Task ExecuteAsync_TransitionsToSucceeded_WhenDataServiceCreatesConfig()
    {
        var vpnData = new Mock<IVpnDataService>(MockBehavior.Strict);
        vpnData.Setup(s => s.RunPostAddSetupAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServerPostSetupExecutionResult
            {
                VpnServerId = 40,
                ServerType = VpnServerType.OpenVpn,
                CreatedDefaultConfig = true
            });

        var svc = CreateService(vpnData, out var provider);
        using (provider)
        {
            var started = await svc.StartAsync(40, CancellationToken.None);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            VpnServerPostSetupStatus? final = null;
            while (DateTime.UtcNow < deadline)
            {
                final = await svc.GetStatusAsync(40, started.OperationId, CancellationToken.None);
                if (final?.State is VpnServerPostSetupState.Succeeded or VpnServerPostSetupState.Failed)
                    break;
                await Task.Delay(50);
            }

            Assert.NotNull(final);
            Assert.Equal(VpnServerPostSetupState.Succeeded, final!.State);
            Assert.Equal("completed", final.CurrentStep);
            Assert.Equal("True", final.Details["createdDefaultConfig"]);
            Assert.Equal(VpnServerType.OpenVpn.ToString(), final.Details["serverType"]);
            Assert.NotNull(final.FinishedAtUtc);
            vpnData.Verify(s => s.RunPostAddSetupAsync(40, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_TransitionsToFailed_WhenDataServiceThrows()
    {
        var vpnData = new Mock<IVpnDataService>(MockBehavior.Strict);
        vpnData.Setup(s => s.RunPostAddSetupAsync(41, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("VpnServer not found"));

        var svc = CreateService(vpnData, out var provider);
        using (provider)
        {
            var started = await svc.StartAsync(41, CancellationToken.None);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            VpnServerPostSetupStatus? final = null;
            while (DateTime.UtcNow < deadline)
            {
                final = await svc.GetStatusAsync(41, started.OperationId, CancellationToken.None);
                if (final?.State is VpnServerPostSetupState.Succeeded or VpnServerPostSetupState.Failed)
                    break;
                await Task.Delay(50);
            }

            Assert.NotNull(final);
            Assert.Equal(VpnServerPostSetupState.Failed, final!.State);
            Assert.Equal("failed", final.CurrentStep);
            Assert.Contains("not found", final.Details["error"], StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsNull_WhenUnknownOperationId()
    {
        var vpnData = new Mock<IVpnDataService>(MockBehavior.Strict);
        var svc = CreateService(vpnData, out var provider);
        using (provider)
        {
            var status = await svc.GetStatusAsync(50, "deadbeef", CancellationToken.None);
            Assert.Null(status);
        }
    }
}
