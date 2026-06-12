using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnProxyTrafficFlowSupportCheckerTests
{
    [Fact]
    public async Task ShouldListenAsync_WhenApiUrlMissing_ReturnsFalse()
    {
        var checker = CreateChecker(out var infoService);

        var supported = await checker.ShouldListenAsync(
            new VpnServer { Id = 1, ApiUrl = " ", ServerType = VpnServerType.OpenVpn },
            CancellationToken.None);

        supported.Should().BeFalse();
        infoService.Verify(
            s => s.GetInfoByUrlAsync(It.IsAny<string>(), It.IsAny<VpnServerType?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShouldListenAsync_WhenMicroserviceVersionTooOld_ReturnsFalse()
    {
        var checker = CreateChecker(out var infoService, CreateInfo("1.2.5.53"));

        var supported = await checker.ShouldListenAsync(
            new VpnServer { Id = 7, ApiUrl = "https://ovpn-old", ServerType = VpnServerType.OpenVpn },
            CancellationToken.None);

        supported.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldListenAsync_WhenMicroserviceVersionSupported_ReturnsTrue()
    {
        var checker = CreateChecker(out var infoService, CreateInfo("1.2.5.54"));

        var supported = await checker.ShouldListenAsync(
            new VpnServer { Id = 8, ApiUrl = "https://ovpn-new", ServerType = VpnServerType.OpenVpn },
            CancellationToken.None);

        supported.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldListenAsync_WhenInfoUnavailable_ReturnsFalse()
    {
        var checker = CreateChecker(out var infoService, null);

        var supported = await checker.ShouldListenAsync(
            new VpnServer { Id = 9, ApiUrl = "https://ovpn-missing", ServerType = VpnServerType.OpenVpn },
            CancellationToken.None);

        supported.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldListenAsync_CachesResult_ForSameServerAndApiUrl()
    {
        var checker = CreateChecker(out var infoService, CreateInfo("1.2.5.55"));
        var server = new VpnServer { Id = 10, ApiUrl = "https://ovpn-cache", ServerType = VpnServerType.OpenVpn };

        (await checker.ShouldListenAsync(server, CancellationToken.None)).Should().BeTrue();
        (await checker.ShouldListenAsync(server, CancellationToken.None)).Should().BeTrue();

        infoService.Verify(
            s => s.GetInfoByUrlAsync(server.ApiUrl, VpnServerType.OpenVpn, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static OpenVpnProxyTrafficFlowSupportChecker CreateChecker(
        out Mock<IMicroserviceInfoService> infoService,
        VpnMicroserviceDiagnosticsDto? info = null)
    {
        infoService = new Mock<IMicroserviceInfoService>();
        infoService
            .Setup(s => s.GetInfoByUrlAsync(It.IsAny<string>(), It.IsAny<VpnServerType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(info);

        var services = new ServiceCollection();
        services.AddSingleton(infoService.Object);
        var provider = services.BuildServiceProvider();

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return new OpenVpnProxyTrafficFlowSupportChecker(
            scopeFactory.Object,
            Mock.Of<ILogger<OpenVpnProxyTrafficFlowSupportChecker>>());
    }

    private static VpnMicroserviceDiagnosticsDto CreateInfo(string version) =>
        new()
        {
            ServerType = VpnServerType.OpenVpn,
            OpenVpn = new RootOpenVpnInfoResponse { Version = version }
        };
}
