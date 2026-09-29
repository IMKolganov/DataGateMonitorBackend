using Moq;
using DataGateMonitor.Models;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Dto;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DataGateMonitor.Tests.Services.Helpers;

public class VpnServerCoordinateEnricherTests
{
    private static (VpnServerCoordinateEnricher Enricher, Mock<IGeoLiteQueryService> Geo, Mock<GeoLiteDatabaseFactory> Factory)
        Create(bool geoLiteReady = true)
    {
        var geo = new Mock<IGeoLiteQueryService>();
        var factory = new Mock<GeoLiteDatabaseFactory>(
            MockBehavior.Loose,
            NullLogger<GeoLiteDatabaseFactory>.Instance,
            Mock.Of<IServiceProvider>())
        {
            CallBase = false
        };
        factory.Setup(f => f.TryEnsureReadyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(geoLiteReady);

        var enricher = new VpnServerCoordinateEnricher(
            geo.Object,
            factory.Object,
            NullLogger<VpnServerCoordinateEnricher>.Instance);

        return (enricher, geo, factory);
    }

    [Fact]
    public async Task TryFillMissingAsync_DoesNothing_WhenBothCoordinatesAlreadySet()
    {
        var (enricher, geo, factory) = Create();
        var server = new VpnServer
        {
            ApiUrl = "https://8.8.8.8:5010/",
            Latitude = 1,
            Longitude = 2
        };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: "1.2.3.4", CancellationToken.None);

        Assert.Equal(1, server.Latitude);
        Assert.Equal(2, server.Longitude);
        factory.Verify(f => f.TryEnsureReadyAsync(It.IsAny<CancellationToken>()), Times.Never);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(10.0, null)]
    [InlineData(null, 20.0)]
    public async Task TryFillMissingAsync_DoesNothing_WhenOnlyOneCoordinateSet(double? lat, double? lon)
    {
        var (enricher, geo, factory) = Create();
        var server = new VpnServer
        {
            ApiUrl = "https://8.8.8.8:5010/",
            Latitude = lat,
            Longitude = lon
        };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: "1.2.3.4", CancellationToken.None);

        Assert.Equal(lat, server.Latitude);
        Assert.Equal(lon, server.Longitude);
        factory.Verify(f => f.TryEnsureReadyAsync(It.IsAny<CancellationToken>()), Times.Never);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryFillMissingAsync_Skips_WhenGeoLiteNotReady()
    {
        var (enricher, geo, factory) = Create(geoLiteReady: false);
        var server = new VpnServer { ApiUrl = "https://203.0.113.10/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None);

        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
        factory.Verify(f => f.TryEnsureReadyAsync(It.IsAny<CancellationToken>()), Times.Once);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryFillMissingAsync_DoesNotThrow_WhenTryEnsureReadyThrows()
    {
        var (enricher, geo, factory) = Create();
        factory.Setup(f => f.TryEnsureReadyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("factory boom"));
        var server = new VpnServer { ApiUrl = "https://203.0.113.10/" };

        var ex = await Record.ExceptionAsync(() =>
            enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None));

        Assert.Null(ex);
        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryFillMissingAsync_UsesPreferredPublicIp_OverApiUrlHost()
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync("203.0.113.10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Latitude = 55.75, Longitude = 37.62 });

        var server = new VpnServer { ApiUrl = "https://198.51.100.1:5010/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: "203.0.113.10", CancellationToken.None);

        Assert.Equal(55.75, server.Latitude);
        Assert.Equal(37.62, server.Longitude);
        geo.Verify(g => g.GetGeoInfoAsync("203.0.113.10", It.IsAny<CancellationToken>()), Times.Once);
        geo.Verify(g => g.GetGeoInfoAsync("198.51.100.1", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-ip")]
    [InlineData("http://evil.example/")]
    public async Task TryFillMissingAsync_FallsBackToApiUrlIp_WhenPreferredIpUnusable(string? preferred)
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync("198.51.100.50", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Latitude = 52.37, Longitude = 4.90 });

        var server = new VpnServer { ApiUrl = "https://198.51.100.50:5010/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: preferred, CancellationToken.None);

        Assert.Equal(52.37, server.Latitude);
        Assert.Equal(4.90, server.Longitude);
        geo.Verify(g => g.GetGeoInfoAsync("198.51.100.50", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryFillMissingAsync_UsesApiUrlIp_WhenNoPreferredIp()
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync("198.51.100.50", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Latitude = 52.37, Longitude = 4.90 });

        var server = new VpnServer { ApiUrl = "https://198.51.100.50:5010/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None);

        Assert.Equal(52.37, server.Latitude);
        Assert.Equal(4.90, server.Longitude);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    public async Task TryFillMissingAsync_LeavesCoordsUnset_WhenNoResolvableIp(string? apiUrl)
    {
        var (enricher, geo, _) = Create();
        var server = new VpnServer { ApiUrl = apiUrl! };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None);

        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryFillMissingAsync_SkipsHostname_WithoutDns_WhenNoPreferredIp()
    {
        var (enricher, geo, _) = Create();
        // Hostname in ApiUrl must not trigger DNS — save path stays local-only.
        var server = new VpnServer { ApiUrl = "https://s5.datagateapp.com/" };

        var ex = await Record.ExceptionAsync(() =>
            enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None));

        Assert.Null(ex);
        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryFillMissingAsync_UsesPreferredIp_WhenApiUrlIsHostname()
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync("203.0.113.44", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Latitude = 40.7, Longitude = -74.0 });

        var server = new VpnServer { ApiUrl = "https://s5.datagateapp.com/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: "203.0.113.44", CancellationToken.None);

        Assert.Equal(40.7, server.Latitude);
        Assert.Equal(-74.0, server.Longitude);
    }

    [Fact]
    public async Task TryFillMissingAsync_SkipsNullIsland_FromPrivateIpLookup()
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync("10.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Latitude = 0, Longitude = 0, City = "RFC1918" });

        var server = new VpnServer { ApiUrl = "https://10.0.0.1:5010/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None);

        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
    }

    [Theory]
    [InlineData(null, 10.0)]
    [InlineData(10.0, null)]
    public async Task TryFillMissingAsync_LeavesCoordsUnset_WhenGeoReturnsIncompletePair(double? lat, double? lon)
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync("203.0.113.99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Latitude = lat, Longitude = lon });

        var server = new VpnServer { ApiUrl = "https://203.0.113.99/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None);

        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
    }

    [Fact]
    public async Task TryFillMissingAsync_LeavesCoordsUnset_WhenGeoLiteReturnsNull()
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync("203.0.113.99", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenVpnGeoInfo?)null);

        var server = new VpnServer { ApiUrl = "https://203.0.113.99/" };

        await enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None);

        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
    }

    [Fact]
    public async Task TryFillMissingAsync_DoesNotThrow_WhenGeoLiteThrows()
    {
        var (enricher, geo, _) = Create();
        geo.Setup(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var server = new VpnServer { ApiUrl = "https://203.0.113.7/" };

        var ex = await Record.ExceptionAsync(() =>
            enricher.TryFillMissingAsync(server, preferredPublicIp: null, CancellationToken.None));

        Assert.Null(ex);
        Assert.Null(server.Latitude);
        Assert.Null(server.Longitude);
    }

    [Fact]
    public async Task TryFillMissingAsync_PropagatesCallerCancellation()
    {
        var (enricher, _, factory) = Create();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        factory.Setup(f => f.TryEnsureReadyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var server = new VpnServer { ApiUrl = "https://203.0.113.7/" };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            enricher.TryFillMissingAsync(server, preferredPublicIp: null, cts.Token));
    }
}
