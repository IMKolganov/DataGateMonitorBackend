using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;
using Xunit;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class OvpnFileApiClientTests
{
    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri("https://vpn.test/") };
    }

    [Fact]
    public async Task AddOvpnFile_WhenServerNotFound_Throws()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Loose);
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServer?)null);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Loose);
        var logger = Mock.Of<ILogger<OvpnFileApiClient>>();
        var sut = new OvpnFileApiClient(httpFactory.Object, serverQuery.Object, tokenService.Object, logger);
        var request = new GenerateOvpnFileRequest { CommonName = "cn" };

        var act = () => sut.AddOvpnFile(99, request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*VPN server 99 not found*");
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task AddOvpnFile_WhenServerHasNoApiUrl_ThrowsInvalidOperationException()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Loose);
        var server = new VpnServer { Id = 1, ApiUrl = null!, ServerName = "S1", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow };
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Loose);
        var logger = Mock.Of<ILogger<OvpnFileApiClient>>();
        var sut = new OvpnFileApiClient(httpFactory.Object, serverQuery.Object, tokenService.Object, logger);
        var request = new GenerateOvpnFileRequest { CommonName = "cn" };

        var act = () => sut.AddOvpnFile(1, request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*API url is missing*");
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task AddOvpnFile_WhenSuccess_ReturnsMetadata()
    {
        var metadata = new OvpnFileMetadata { CommonName = "user@client", FileName = "user.ovpn", FilePath = "/path/user.ovpn" };
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(metadata) };
        var client = CreateMockHttpClient(response);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var server = new VpnServer { Id = 1, ApiUrl = "https://vpn.test/", ServerName = "S1", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow };
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Strict);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns("jwt");
        var logger = Mock.Of<ILogger<OvpnFileApiClient>>();
        var sut = new OvpnFileApiClient(httpFactory.Object, serverQuery.Object, tokenService.Object, logger);
        var request = new GenerateOvpnFileRequest { CommonName = "user@client" };

        var result = await sut.AddOvpnFile(1, request, CancellationToken.None);

        result.Should().NotBeNull();
        result.CommonName.Should().Be("user@client");
        result.FileName.Should().Be("user.ovpn");
        result.FilePath.Should().Be("/path/user.ovpn");
    }

    [Fact]
    public async Task DownloadOvpnFile_WhenSuccess_ReturnsContent()
    {
        var download = new OvpnFileDownload { CommonName = "cn", FileName = "f.ovpn", Content = new byte[] { 1, 2, 3 } };
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(download) };
        var client = CreateMockHttpClient(response);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var server = new VpnServer { Id = 1, ApiUrl = "https://vpn.test/", ServerName = "S1", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow };
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Strict);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns("jwt");
        var logger = Mock.Of<ILogger<OvpnFileApiClient>>();
        var sut = new OvpnFileApiClient(httpFactory.Object, serverQuery.Object, tokenService.Object, logger);
        var request = new DownloadOvpnFileRequest { CommonName = "cn", FileName = "f.ovpn", FilePath = "/p" };

        var result = await sut.DownloadOvpnFile(1, request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }

    private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
