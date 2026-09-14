using System.Net;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Requests;
using DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Responses;
using DataGateMonitor.SharedModels.Responses;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateXRayManager.ClientLinks;

public class XrayClientLinkMicroserviceClientTests
{
    private static HttpResponseMessage CreateSuccessResponse<T>(T data) where T : class =>
        new(HttpStatusCode.OK) { Content = ProjectJson.ToJsonContent(ApiResponse<T>.SuccessResponse(data)) };

    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri("https://xray.test/") };
    }

    [Fact]
    public async Task AddClientLink_WhenServerNotFound_Throws()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Loose);
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServer?)null);
        var sut = new XrayClientLinkMicroserviceClient(
            httpFactory.Object, serverQuery.Object, Mock.Of<IMicroserviceTokenService>(),
            Mock.Of<ILogger<XrayClientLinkMicroserviceClient>>());

        var act = () => sut.AddClientLink(99, new GenerateClientLinkRequest { CommonName = "cn" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*VPN server 99 not found*");
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task AddClientLink_WhenServerHasNoApiUrl_Throws()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 1, ApiUrl = null!, ServerName = "S1" });
        var sut = new XrayClientLinkMicroserviceClient(
            new Mock<IHttpClientFactory>(MockBehavior.Loose).Object, serverQuery.Object,
            Mock.Of<IMicroserviceTokenService>(), Mock.Of<ILogger<XrayClientLinkMicroserviceClient>>());

        var act = () => sut.AddClientLink(1, new GenerateClientLinkRequest { CommonName = "cn" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*API url is missing*");
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task AddClientLink_WhenSuccess_ReturnsMetadata()
    {
        var metadata = new ClientLinkMetadata
        {
            CommonName = "user@xray",
            FileName = "user.txt",
            FilePath = "/path/user.txt"
        };
        var client = CreateMockHttpClient(CreateSuccessResponse(metadata));
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 1, ApiUrl = "https://xray.test/", ServerName = "S1" });
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Strict);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>())).Returns("jwt");
        var sut = new XrayClientLinkMicroserviceClient(
            httpFactory.Object, serverQuery.Object, tokenService.Object,
            Mock.Of<ILogger<XrayClientLinkMicroserviceClient>>());

        var result = await sut.AddClientLink(1, new GenerateClientLinkRequest { CommonName = "user@xray" },
            CancellationToken.None);

        result.CommonName.Should().Be("user@xray");
        result.FileName.Should().Be("user.txt");
        result.FilePath.Should().Be("/path/user.txt");
    }

    [Fact]
    public async Task DownloadClientLink_WhenSuccess_ReturnsContent()
    {
        var download = new ClientLinkDownload { FileName = "f.txt", Content = [1, 2, 3] };
        var client = CreateMockHttpClient(CreateSuccessResponse(download));
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 1, ApiUrl = "https://xray.test/", ServerName = "S1" });
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Strict);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>())).Returns("jwt");
        var sut = new XrayClientLinkMicroserviceClient(
            httpFactory.Object, serverQuery.Object, tokenService.Object,
            Mock.Of<ILogger<XrayClientLinkMicroserviceClient>>());

        var result = await sut.DownloadClientLink(1, new DownloadClientLinkRequest
        {
            CommonName = "cn",
            FileName = "f.txt",
            FilePath = "/p"
        }, CancellationToken.None);

        result.Content.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task RevokeClientLink_WhenHttpFails_Throws()
    {
        var client = CreateMockHttpClient(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = ProjectJson.ToJsonContent(ApiResponse<ClientLinkMetadata>.ErrorResponse("nope"))
        });
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 1, ApiUrl = "https://xray.test/", ServerName = "S1" });
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Strict);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>())).Returns("jwt");
        var sut = new XrayClientLinkMicroserviceClient(
            httpFactory.Object, serverQuery.Object, tokenService.Object,
            Mock.Of<ILogger<XrayClientLinkMicroserviceClient>>());

        var act = () => sut.RevokeClientLink(1, new RevokeClientLinkRequest { CommonName = "cn" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Failed to revoke client link*");
    }

    private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
