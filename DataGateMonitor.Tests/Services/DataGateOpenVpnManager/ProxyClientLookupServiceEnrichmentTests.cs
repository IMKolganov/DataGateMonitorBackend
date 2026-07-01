using System.Net;
using System.Text;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Dto;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class ProxyClientLookupServiceEnrichmentTests
{
    private static ProxyClientLookupService CreateService(
        HttpMessageHandler handler,
        out Mock<IGeoLiteQueryService> geoMock,
        out Mock<IMicroserviceTokenService> tokenMock)
    {
        geoMock = new Mock<IGeoLiteQueryService>();
        tokenMock = new Mock<IMicroserviceTokenService>();
        tokenMock.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt");

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        var notifications = new Mock<IOpenVpnMicroserviceNotificationService>();

        return new ProxyClientLookupService(
            factory.Object,
            tokenMock.Object,
            geoMock.Object,
            notifications.Object,
            NullLogger<ProxyClientLookupService>.Instance);
    }

    private static string WrapLookup(ProxyClientLookupResponse data) =>
        JsonConvert.SerializeObject(ApiResponse<ProxyClientLookupResponse>.SuccessResponse(data));

    [Theory]
    [InlineData("127.0.0.1:53188", 53188)]
    [InlineData("tcp4-server:127.0.0.1:53188", 53188)]
    public async Task EnrichFromManagementRealAddress_Loopback_LegacyAndOpenVpn27_LookupAndGeo(string remoteIp, int localPort)
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(WrapLookup(new ProxyClientLookupResponse
                {
                    RealClientIp = "198.51.100.10",
                    RealClientPort = 8443
                }), Encoding.UTF8, "application/json")
            };
        });

        var sut = CreateService(handler, out var geoMock, out _);
        geoMock.Setup(x => x.GetGeoInfoAsync("198.51.100.10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Country = "US", Region = "CA", City = "LA" });

        var client = new VpnServerClient { CommonName = "cn", RemoteIp = remoteIp };
        var server = new VpnServer { Id = 75, ApiUrl = "https://vpn.example.com", ServerName = "test" };

        await sut.EnrichFromManagementRealAddressAsync(server, client, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Contains($"localPort={localPort}", captured!.RequestUri!.Query);
        Assert.Contains("host=127.0.0.1", captured.RequestUri.Query);
        Assert.Equal("198.51.100.10:8443", client.ProxyRealIp);
        Assert.Equal("US", client.Country);
        geoMock.Verify(x => x.GetGeoInfoAsync("198.51.100.10", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrichFromManagementRealAddress_LoopbackWithoutProxyMatch_SkipsGeo()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                JsonConvert.SerializeObject(ApiResponse<object?>.ErrorResponse("No active proxy session for the given local port.")),
                Encoding.UTF8,
                "application/json")
        });

        var sut = CreateService(handler, out var geoMock, out _);
        var client = new VpnServerClient { CommonName = "cn", RemoteIp = "tcp4-server:127.0.0.1:55664" };
        var server = new VpnServer { Id = 75, ApiUrl = "https://vpn.example.com" };

        await sut.EnrichFromManagementRealAddressAsync(server, client, CancellationToken.None);

        Assert.Null(client.ProxyRealIp);
        Assert.Null(client.Country);
        geoMock.Verify(x => x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrichFromManagementRealAddress_DirectExternal_UsesRemoteIpForGeo()
    {
        var sut = CreateService(new StubHandler(_ => throw new InvalidOperationException("no lookup")), out var geoMock, out _);
        geoMock.Setup(x => x.GetGeoInfoAsync("203.0.113.5:443", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Country = "DE" });

        var client = new VpnServerClient { CommonName = "cn", RemoteIp = "203.0.113.5:443" };
        var server = new VpnServer { Id = 1, ApiUrl = "https://vpn.example.com" };

        await sut.EnrichFromManagementRealAddressAsync(server, client, CancellationToken.None);

        Assert.Equal("DE", client.Country);
        geoMock.Verify(x => x.GetGeoInfoAsync("203.0.113.5:443", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrichFromManagementRealAddress_OpenVpn27External_PassesFullStringToGeo()
    {
        var sut = CreateService(new StubHandler(_ => throw new InvalidOperationException("no lookup")), out var geoMock, out _);
        geoMock.Setup(x => x.GetGeoInfoAsync("tcp4-server:203.0.113.5:443", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo { Country = "DE" });

        var client = new VpnServerClient { CommonName = "cn", RemoteIp = "tcp4-server:203.0.113.5:443" };
        var server = new VpnServer { Id = 1, ApiUrl = "https://vpn.example.com" };

        await sut.EnrichFromManagementRealAddressAsync(server, client, CancellationToken.None);

        Assert.Equal("DE", client.Country);
        geoMock.Verify(x => x.GetGeoInfoAsync("tcp4-server:203.0.113.5:443", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrichFromManagementRealAddress_LoopbackWithoutApiUrl_SkipsLookupAndGeo()
    {
        var sut = CreateService(new StubHandler(_ => throw new InvalidOperationException("no http")), out var geoMock, out _);
        var client = new VpnServerClient { CommonName = "cn", RemoteIp = "tcp4-server:127.0.0.1:55664" };
        var server = new VpnServer { Id = 75, ApiUrl = "" };

        await sut.EnrichFromManagementRealAddressAsync(server, client, CancellationToken.None);

        Assert.Null(client.ProxyRealIp);
        geoMock.Verify(x => x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
