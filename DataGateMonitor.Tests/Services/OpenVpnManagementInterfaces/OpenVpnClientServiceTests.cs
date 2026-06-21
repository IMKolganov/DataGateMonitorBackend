using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Services.OpenVpnManagementInterfaces;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Dto;

namespace DataGateMonitor.Tests.Services.OpenVpnManagementInterfaces;

public class OpenVpnClientServiceTests
{
    private static OpenVpnClientService CreateService(
        out Mock<IGeoLiteQueryService> geoMock,
        out Mock<IOpenVpnMicroserviceClientFactory> factoryMock,
        out Mock<ILogger<IOpenVpnClientService>> loggerMock,
        out Mock<IProxyClientLookupService> proxyMock)
    {
        loggerMock = new Mock<ILogger<IOpenVpnClientService>>();
        geoMock = new Mock<IGeoLiteQueryService>(MockBehavior.Strict);
        factoryMock = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);
        proxyMock = new Mock<IProxyClientLookupService>(MockBehavior.Strict);
        var geoService = geoMock.Object;
        proxyMock
            .Setup(x => x.EnrichFromManagementRealAddressAsync(It.IsAny<VpnServer>(), It.IsAny<VpnServerClient>(),
                It.IsAny<CancellationToken>()))
            .Returns<VpnServer, VpnServerClient, CancellationToken>((_, client, ct) =>
                DefaultEnrichFromGeoAsync(geoService, client, ct));

        return new OpenVpnClientService(loggerMock.Object, factoryMock.Object, proxyMock.Object);
    }

    /// <summary>Mimics the non-loopback path: GeoLite on management <see cref="VpnServerClient.RemoteIp"/> only.</summary>
    private static async Task DefaultEnrichFromGeoAsync(IGeoLiteQueryService geo, VpnServerClient client,
        CancellationToken ct)
    {
        var geoInfo = await geo.GetGeoInfoAsync(client.RemoteIp, ct);
        if (geoInfo is not null)
        {
            client.Country = geoInfo.Country;
            client.Region = geoInfo.Region;
            client.City = geoInfo.City;
            client.Latitude = geoInfo.Latitude;
            client.Longitude = geoInfo.Longitude;
        }

        client.ProxyRealIp = null;
    }

    private static async Task<OpenVpnManagementStatusResult> InvokeParseStatusAsync(OpenVpnClientService svc, string data,
        VpnServer? server = null)
    {
        server ??= new VpnServer { Id = 1, ApiUrl = "https://example.com" };
        var mi = typeof(OpenVpnClientService)
            .GetMethod("ParseStatus", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task<OpenVpnManagementStatusResult>)mi.Invoke(svc, new object[] { data, server, CancellationToken.None })!;
        return await task.ConfigureAwait(false);
    }

    [Fact]
    public async Task ParseStatus_WithSampleResponse_ParsesClients_And_EnrichesGeo_And_SetsDeterministicSessionId()
    {
        // Arrange
        var svc = CreateService(out var geoMock, out var factoryMock, out _, out _);

        // Geo mocks per remote IP: both are used once per client (anonymized IPs)
        geoMock.Setup(x => x.GetGeoInfoAsync("203.0.113.5:40884", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OpenVpnGeoInfo
               {
                   Country = "DE",
                   Region = "Berlin",
                   City = "Berlin",
                   Latitude = 52.52,
                   Longitude = 13.405
               });
        geoMock.Setup(x => x.GetGeoInfoAsync("198.51.100.23:2093", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OpenVpnGeoInfo
               {
                   Country = "RU",
                   Region = "Moscow",
                   City = "Moscow",
                   Latitude = 55.7558,
                   Longitude = 37.6173
               });

        const string sample = "TITLE\tOpenVPN 2.6.17 x86_64-pc-linux-gnu [SSL (OpenSSL)] [LZO] [LZ4] [EPOLL] [MH/PKTINFO] [AEAD] [DCO]\n" +
                              "TIME\t2025-12-03 09:00:06\t1764752406\n" +
                              "HEADER\tCLIENT_LIST\tCommon Name\tReal Address\tVirtual Address\tVirtual IPv6 Address\tBytes Received\tBytes Sent\tConnected Since\tConnected Since (time_t)\tUsername\tClient ID\tPeer ID\tData Channel Cipher\n" +
                              "CLIENT_LIST\tclient-a\t203.0.113.5:40884\t10.51.28.2\t\t63718808\t451843082\t2025-12-03 08:55:35\t1764752135\tUNDEF\t0\t0\tAES-256-GCM\n" +
                              "CLIENT_LIST\tclient-b\t198.51.100.23:2093\t10.51.28.3\t\t49819\t91528\t2025-12-03 08:59:55\t1764752395\tUNDEF\t1\t1\tAES-256-GCM\n" +
                              "HEADER\tROUTING_TABLE\tVirtual Address\tCommon Name\tReal Address\tLast Ref\tLast Ref (time_t)\n" +
                              "ROUTING_TABLE\t10.51.28.2\tclient-a\t203.0.113.5:40884\t2025-12-03 08:55:36\t1764752136\n" +
                              "ROUTING_TABLE\t10.51.28.3\tclient-b\t198.51.100.23:2093\t2025-12-03 08:59:55\t1764752395\n" +
                              "GLOBAL_STATS\tMax bcast/mcast queue length\t0\n" +
                              "GLOBAL_STATS\tdco_enabled\t1\n" +
                              "END\n";

        // Act
        var parseResult = await InvokeParseStatusAsync(svc, sample);
        var clientsFirst = parseResult.Clients;

        // Assert base parsing
        Assert.Equal(2, clientsFirst.Count);
        Assert.True(parseResult.DcoEnabled); // GLOBAL_STATS dco_enabled 1

        var c1 = clientsFirst[0];
        Assert.Equal("client-a", c1.CommonName);
        Assert.Equal("203.0.113.5:40884", c1.RemoteIp);
        Assert.Equal("10.51.28.2", c1.LocalIp);
        Assert.Equal(63718808, c1.BytesReceived);
        Assert.Equal(451843082, c1.BytesSent);
        Assert.Equal("client-a", c1.Username); // UNDEF -> fallback to CommonName
        Assert.Equal(new DateTimeOffset(2025, 12, 3, 8, 55, 35, TimeSpan.Zero), c1.ConnectedSince);
        Assert.Equal("DE", c1.Country);
        Assert.Equal("Berlin", c1.Region);
        Assert.Equal("Berlin", c1.City);
        Assert.Equal(52.52, c1.Latitude);
        Assert.Equal(13.405, c1.Longitude);

        var c2 = clientsFirst[1];
        Assert.Equal("client-b", c2.CommonName);
        Assert.Equal("198.51.100.23:2093", c2.RemoteIp);
        Assert.Equal("10.51.28.3", c2.LocalIp);
        Assert.Equal(49819, c2.BytesReceived);
        Assert.Equal(91528, c2.BytesSent);
        Assert.Equal("client-b", c2.Username);
        Assert.Equal(new DateTimeOffset(2025, 12, 3, 8, 59, 55, TimeSpan.Zero), c2.ConnectedSince);
        Assert.Equal("RU", c2.Country);
        Assert.Equal("Moscow", c2.Region);
        Assert.Equal("Moscow", c2.City);
        Assert.Equal(55.7558, c2.Latitude);
        Assert.Equal(37.6173, c2.Longitude);

        Assert.NotEqual(Guid.Empty, c1.SessionId);
        Assert.NotEqual(Guid.Empty, c2.SessionId);

        // Act again to verify SessionId stability
        var resultSecond = await InvokeParseStatusAsync(svc, sample);
        Assert.Equal(2, resultSecond.Clients.Count);

        Assert.Equal(c1.SessionId, resultSecond.Clients[0].SessionId);
        Assert.Equal(c2.SessionId, resultSecond.Clients[1].SessionId);

        // Verify geo lookup calls occurred (2 clients x 2 invocations = 4 calls)
        geoMock.Verify(x => x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task ParseStatus_GLOBAL_STATS_DcoEnabledZero_SetsDcoEnabledFalse()
    {
        var svc = CreateService(out _, out _, out _, out _);
        const string sample = "GLOBAL_STATS\tdco_enabled\t0\nEND\n";
        var result = await InvokeParseStatusAsync(svc, sample);
        Assert.Empty(result.Clients);
        Assert.False(result.DcoEnabled);
    }

    [Fact]
    public async Task ParseStatus_NoClientListLines_ReturnsEmpty_And_NoGeoCalls()
    {
        var svc = CreateService(out var geoMock, out _, out _, out _);

        const string sample = "TITLE\tOpenVPN\nHEADER\tROUTING_TABLE\t...\nEND\n";
        var result = await InvokeParseStatusAsync(svc, sample);

        Assert.Empty(result.Clients);
        Assert.Null(result.DcoEnabled);
        geoMock.Verify(x => x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ParseStatus_GeoReturnsNull_ClientParsedWithoutGeo()
    {
        var svc = CreateService(out var geoMock, out _, out _, out _);

        geoMock.Setup(x => x.GetGeoInfoAsync("203.0.113.5:1111", It.IsAny<CancellationToken>()))
               .ReturnsAsync((OpenVpnGeoInfo?)null);

        const string sample =
            "CLIENT_LIST\tclient-x\t203.0.113.5:1111\t10.0.0.2\t\t100\t200\t2025-12-03 08:00:00\t1764751200\tUNDEF\t0\t0\tAES-256-GCM\n";

        var result = await InvokeParseStatusAsync(svc, sample);
        Assert.Single(result.Clients);
        var c = result.Clients[0];
        Assert.Equal("client-x", c.CommonName);
        Assert.Equal("203.0.113.5:1111", c.RemoteIp);
        Assert.Equal("10.0.0.2", c.LocalIp);
        Assert.Equal(100, c.BytesReceived);
        Assert.Equal(200, c.BytesSent);
        Assert.Equal("client-x", c.Username);
        Assert.Null(c.Country);
        Assert.Null(c.Region);
        Assert.Null(c.City);
        Assert.Null(c.Latitude);
        Assert.Null(c.Longitude);
        Assert.NotEqual(Guid.Empty, c.SessionId);
    }

    [Fact]
    public async Task ParseStatus_UsernameProvided_UsesUsernameField()
    {
        var svc = CreateService(out var geoMock, out _, out _, out _);
        geoMock.Setup(x => x.GetGeoInfoAsync("198.51.100.10:2222", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OpenVpnGeoInfo());

        const string sample =
            "CLIENT_LIST\tclient-y\t198.51.100.10:2222\t10.0.0.3\t\t1\t2\t2025-12-03 09:10:11\t1764753011\tuser-y\t0\t0\tAES-256-GCM\n";

        var result = await InvokeParseStatusAsync(svc, sample);
        Assert.Single(result.Clients);
        Assert.Equal("user-y", result.Clients[0].Username);
    }

    [Fact]
    public async Task ParseStatus_InsufficientColumns_IsSkipped()
    {
        var svc = CreateService(out var geoMock, out _, out _, out _);
        const string sample = "CLIENT_LIST\tclient-z\t203.0.113.9:3333\t10.0.0.4\n"; // < 8 columns
        var result = await InvokeParseStatusAsync(svc, sample);
        Assert.Empty(result.Clients);
        geoMock.Verify(x => x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        var mi = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (T)mi.Invoke(instance, args)!;
    }

    [Fact]
    public void TryParseLong_Empty_ReturnsZero_And_LogsWarning()
    {
        var svc = CreateService(out _, out _, out var loggerMock, out _);
        var result = InvokePrivate<long>(svc, "TryParseLong", " \t", "BytesReceived");
        Assert.Equal(0, result);

        loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("BytesReceived is empty")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void TryParseLong_Invalid_Throws_And_LogsError()
    {
        var svc = CreateService(out _, out _, out var loggerMock, out _);
        var ex = Assert.Throws<TargetInvocationException>(() => InvokePrivate<long>(svc, "TryParseLong", "abc", "BytesSent"));
        Assert.IsType<FormatException>(ex.InnerException);

        loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("Failed to parse BytesSent")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void TryParseInstantUtc_Empty_ReturnsMin_And_LogsWarning()
    {
        var svc = CreateService(out _, out _, out var loggerMock, out _);
        var dto = InvokePrivate<DateTimeOffset>(svc, "TryParseInstantUtc", " ", "ConnectedSince");
        Assert.Equal(DateTimeOffset.MinValue, dto);
        loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("ConnectedSince is empty")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void TryParseInstantUtc_UnixSeconds_Parses()
    {
        var svc = CreateService(out _, out _, out _, out _);
        var dto = InvokePrivate<DateTimeOffset>(svc, "TryParseInstantUtc", "1764752135", "ConnectedSince");
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1764752135), dto);
    }

    [Fact]
    public void TryParseInstantUtc_Iso8601WithOffset_NormalizedToUtc()
    {
        var svc = CreateService(out _, out _, out _, out _);
        var dto = InvokePrivate<DateTimeOffset>(svc, "TryParseInstantUtc", "2025-12-03T10:00:00+02:00", "ConnectedSince");
        Assert.Equal(new DateTimeOffset(2025, 12, 3, 8, 0, 0, TimeSpan.Zero), dto);
    }

    [Fact]
    public void TryParseInstantUtc_Invalid_Throws_And_LogsError()
    {
        var svc = CreateService(out _, out _, out var loggerMock, out _);
        var ex = Assert.Throws<TargetInvocationException>(() => InvokePrivate<DateTimeOffset>(svc, "TryParseInstantUtc", "not-a-date", "ConnectedSince"));
        Assert.IsType<FormatException>(ex.InnerException);
        loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("Failed to parse ConnectedSince")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ParseStatus_InvalidFirstLine_Skipped_SecondValidParsed()
    {
        var svc = CreateService(out var geoMock, out _, out _, out _);
        geoMock.Setup(x => x.GetGeoInfoAsync("203.0.113.5:1234", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OpenVpnGeoInfo { Country = "X" });

        const string sample =
            "CLIENT_LIST\tbad\t203.0.113.9:9999\t10.0.0.9\t\tNOPE\t2\t2025-12-03 01:01:01\t1764750001\tUNDEF\t0\t0\tAES\n" +
            "CLIENT_LIST\tok\t203.0.113.5:1234\t10.0.0.8\t\t10\t20\t2025-12-03 02:02:02\t1764750322\tUNDEF\t0\t0\tAES\n";

        var result = await InvokeParseStatusAsync(svc, sample);
        Assert.Single(result.Clients);
        Assert.Equal("ok", result.Clients[0].CommonName);
        Assert.Equal("203.0.113.5:1234", result.Clients[0].RemoteIp);
        geoMock.Verify(x => x.GetGeoInfoAsync("203.0.113.5:1234", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetClientsFromManagementAsync_HappyPath_ParsesViaClient_And_Logs()
    {
        // Arrange
        var logger = new Mock<ILogger<IOpenVpnClientService>>();
        var geo = new Mock<IGeoLiteQueryService>(MockBehavior.Strict);
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);
        var client = new Mock<IOpenVpnMicroserviceClient>(MockBehavior.Strict);
        var proxy = new Mock<IProxyClientLookupService>(MockBehavior.Strict);
        proxy.Setup(x => x.EnrichFromManagementRealAddressAsync(It.IsAny<VpnServer>(), It.IsAny<VpnServerClient>(),
                It.IsAny<CancellationToken>()))
            .Returns<VpnServer, VpnServerClient, CancellationToken>((_, cl, ct) =>
                DefaultEnrichFromGeoAsync(geo.Object, cl, ct));

        var svc = new OpenVpnClientService(logger.Object, factory.Object, proxy.Object);

        var server = new VpnServer { Id = 123, ApiUrl = "https://api.example" };

        const string sample =
            "TITLE\tOpenVPN\n" +
            "HEADER\tCLIENT_LIST\tCommon Name\tReal Address\tVirtual Address\tVirtual IPv6 Address\tBytes Received\tBytes Sent\tConnected Since\tConnected Since (time_t)\tUsername\tClient ID\tPeer ID\tData Channel Cipher\n" +
            "CLIENT_LIST\tclient-a\t203.0.113.5:40884\t10.51.28.2\t\t10\t20\t2025-12-03 08:55:35\t1764752135\tUNDEF\t0\t0\tAES\n" +
            "CLIENT_LIST\tclient-b\t198.51.100.23:2093\t10.51.28.3\t\t30\t40\t2025-12-03 08:59:55\t1764752395\tUNDEF\t1\t1\tAES\n" +
            "END\n";

        factory.Setup(f => f.Create(server)).Returns(client.Object);
        client.Setup(c => c.SendCommandWithResponseAsync("status 3", It.IsAny<CancellationToken>()))
              .ReturnsAsync(sample);

        geo.Setup(g => g.GetGeoInfoAsync("203.0.113.5:40884", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new OpenVpnGeoInfo { Country = "DE" });
        geo.Setup(g => g.GetGeoInfoAsync("198.51.100.23:2093", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new OpenVpnGeoInfo { Country = "RU" });

        // Act
        var result = await svc.GetClientsFromManagementAsync(server, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Clients.Count);
        Assert.Equal("client-a", result.Clients[0].CommonName);
        Assert.Equal("client-b", result.Clients[1].CommonName);
        Assert.Equal("DE", result.Clients[0].Country);
        Assert.Equal("RU", result.Clients[1].Country);

        factory.Verify(f => f.Create(server), Times.Once);
        client.Verify(c => c.SendCommandWithResponseAsync("status 3", It.IsAny<CancellationToken>()), Times.Once);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Verify logging: Received status response (Debug) and Found N (Information)
        logger.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("Received status response")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("Found 2 connected clients")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetClientsFromManagementAsync_EmptyResponse_ReturnsEmpty_And_NoGeoCalls()
    {
        // Arrange
        var logger = new Mock<ILogger<IOpenVpnClientService>>();
        var geo = new Mock<IGeoLiteQueryService>(MockBehavior.Strict);
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);
        var client = new Mock<IOpenVpnMicroserviceClient>(MockBehavior.Strict);
        var proxy = new Mock<IProxyClientLookupService>(MockBehavior.Strict);
        var svc = new OpenVpnClientService(logger.Object, factory.Object, proxy.Object);
        var server = new VpnServer { Id = 1, ApiUrl = "https://api.example" };

        factory.Setup(f => f.Create(server)).Returns(client.Object);
        client.Setup(c => c.SendCommandWithResponseAsync("status 3", It.IsAny<CancellationToken>()))
              .ReturnsAsync("TITLE\tOpenVPN\nHEADER\tROUTING_TABLE\nEND\n");

        // Act
        var result = await svc.GetClientsFromManagementAsync(server, CancellationToken.None);

        // Assert
        Assert.Empty(result.Clients);
        geo.Verify(g => g.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        factory.VerifyAll();
        client.VerifyAll();

        logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("Found 0 connected clients")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
