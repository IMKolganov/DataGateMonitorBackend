using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Others.Notifications.CertApiClient;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class CertApiClientTests
{
    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri("https://vpn.test/") };
    }

    [Fact]
    public async Task GetClientForServerAsync_WhenServerNotFound_ThrowsInvalidOperationException()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Loose);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Loose);
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServer?)null);

        var notification = new Mock<ICertificateNotificationService>(MockBehavior.Loose);
        var logger = Mock.Of<ILogger<CertApiClient>>();
        var sut = new CertApiClient(httpFactory.Object, tokenService.Object, serverQuery.Object,
            notification.Object, logger);

        var act = () => sut.GetAllCertificatesAsync(99, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OpenVPN server not found*");
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task GetAllCertificatesAsync_ReturnsCertificates_AndCallsNotify()
    {
        var certs = new List<ServerCertificate>
        {
            new() { CommonName = "cert1", IsRevoked = false },
            new() { CommonName = "cert2", IsRevoked = true }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(certs)
        };
        var client = CreateMockHttpClient(response);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Strict);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("fake-jwt");
        var server = new VpnServer { Id = 1, ApiUrl = "https://vpn.test/", ServerName = "Test", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow };
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        var notification = new Mock<ICertificateNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyReadAllAsync(1, 2, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var logger = Mock.Of<ILogger<CertApiClient>>();
        var sut = new CertApiClient(httpFactory.Object, tokenService.Object, serverQuery.Object,
            notification.Object, logger);

        var result = await sut.GetAllCertificatesAsync(1, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].CommonName.Should().Be("cert1");
        result[1].CommonName.Should().Be("cert2");
        result[1].IsRevoked.Should().BeTrue();
        notification.Verify(n => n.NotifyReadAllAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildCertificateAsync_ReturnsCertificate_AndCallsNotifyBuilt()
    {
        var cert = new ServerCertificate { CommonName = "new-cert", IsRevoked = false };
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(cert) };
        var client = CreateMockHttpClient(response);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Strict);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns("fake-jwt");
        var server = new VpnServer { Id = 2, ApiUrl = "https://vpn.test/", ServerName = "S2", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow };
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(2, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        var notification = new Mock<ICertificateNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyBuiltAsync(2, It.IsAny<ServerCertificate>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var logger = Mock.Of<ILogger<CertApiClient>>();
        var sut = new CertApiClient(httpFactory.Object, tokenService.Object, serverQuery.Object, notification.Object, logger);

        var result = await sut.BuildCertificateAsync(2, "new-cert", CancellationToken.None);

        result.CommonName.Should().Be("new-cert");
        result.IsRevoked.Should().BeFalse();
        notification.Verify(n => n.NotifyBuiltAsync(2, It.IsAny<ServerCertificate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
