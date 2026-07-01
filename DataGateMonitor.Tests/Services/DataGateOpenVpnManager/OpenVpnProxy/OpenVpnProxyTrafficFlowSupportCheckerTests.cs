using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnProxyTrafficFlowSupportCheckerTests
{
    private static OpenVpnProxyTrafficFlowSupportChecker CreateChecker(
        Mock<IMicroserviceInfoService> infoMock)
    {
        var services = new ServiceCollection();
        services.AddSingleton(infoMock.Object);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        return new OpenVpnProxyTrafficFlowSupportChecker(scopeFactory, NullLogger<OpenVpnProxyTrafficFlowSupportChecker>.Instance);
    }

    private static VpnServer Server(string? apiUrl = "https://vpn.example.com/") =>
        new()
        {
            Id = 75,
            ApiUrl = apiUrl ?? string.Empty,
            ServerType = VpnServerType.OpenVpn,
        };

    private static VpnMicroserviceDiagnosticsDto Diagnostics(string version) =>
        new()
        {
            ServerType = VpnServerType.OpenVpn,
            OpenVpn = new RootOpenVpnInfoResponse { Version = version }
        };

    [Fact]
    public async Task ShouldListenAsync_ReturnsFalse_WhenApiUrlMissing()
    {
        var info = new Mock<IMicroserviceInfoService>(MockBehavior.Strict);
        var sut = CreateChecker(info);

        var result = await sut.ShouldListenAsync(Server(apiUrl: null), CancellationToken.None);

        Assert.False(result);
    }

    [Theory]
    [InlineData("1.2.5.54", true)]
    [InlineData("1.2.5.55", true)]
    [InlineData("1.2.5.53", false)]
    [InlineData("", false)]
    public async Task ShouldListenAsync_ComparesMicroserviceVersion(string version, bool expected)
    {
        var info = new Mock<IMicroserviceInfoService>();
        info.Setup(x => x.GetInfoByUrlAsync(It.IsAny<string>(), VpnServerType.OpenVpn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Diagnostics(version));

        var sut = CreateChecker(info);
        var result = await sut.ShouldListenAsync(Server(), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ShouldListenAsync_UsesCache_OnSecondCall()
    {
        var info = new Mock<IMicroserviceInfoService>();
        info.Setup(x => x.GetInfoByUrlAsync(It.IsAny<string>(), VpnServerType.OpenVpn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Diagnostics("1.2.5.54"));

        var sut = CreateChecker(info);
        var server = Server();

        Assert.True(await sut.ShouldListenAsync(server, CancellationToken.None));
        Assert.True(await sut.ShouldListenAsync(server, CancellationToken.None));

        info.Verify(
            x => x.GetInfoByUrlAsync(It.IsAny<string>(), VpnServerType.OpenVpn, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShouldListenAsync_ReturnsFalse_WhenInfoLookupFails()
    {
        var info = new Mock<IMicroserviceInfoService>();
        info.Setup(x => x.GetInfoByUrlAsync(It.IsAny<string>(), VpnServerType.OpenVpn, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        var sut = CreateChecker(info);
        var result = await sut.ShouldListenAsync(Server(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ShouldListenAsync_Reevaluates_WhenApiUrlChanges()
    {
        var info = new Mock<IMicroserviceInfoService>();
        info.Setup(x => x.GetInfoByUrlAsync(It.IsAny<string>(), VpnServerType.OpenVpn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Diagnostics("1.2.5.54"));

        var sut = CreateChecker(info);
        var server = Server("https://a.example.com/");

        Assert.True(await sut.ShouldListenAsync(server, CancellationToken.None));
        server.ApiUrl = "https://b.example.com/";
        Assert.True(await sut.ShouldListenAsync(server, CancellationToken.None));

        info.Verify(
            x => x.GetInfoByUrlAsync(It.IsAny<string>(), VpnServerType.OpenVpn, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
