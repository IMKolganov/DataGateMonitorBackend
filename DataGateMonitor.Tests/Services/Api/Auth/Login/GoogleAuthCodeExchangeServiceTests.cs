using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.Services.Api.Auth.Login;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Login;

public class GoogleAuthCodeExchangeServiceTests
{
    [Fact]
    public async Task ExchangeCodeForIdTokenAsync_When_CodeNull_ThrowsArgumentException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["GoogleAuth:DesktopClientId"]).Returns("cid");
        config.Setup(c => c["GoogleAuth:DesktopClientSecret"]).Returns("secret");
        var http = new HttpClient();
        var sut = new GoogleAuthCodeExchangeService(http, config.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ExchangeCodeForIdTokenAsync("", "verifier", "https://redirect", CancellationToken.None));

        Assert.Contains("Code is required.", ex.Message);
    }

    [Fact]
    public async Task ExchangeCodeForIdTokenAsync_When_CodeVerifierNull_ThrowsArgumentException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["GoogleAuth:DesktopClientId"]).Returns("cid");
        config.Setup(c => c["GoogleAuth:DesktopClientSecret"]).Returns("secret");
        var http = new HttpClient();
        var sut = new GoogleAuthCodeExchangeService(http, config.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ExchangeCodeForIdTokenAsync("code", "", "https://redirect", CancellationToken.None));

        Assert.Contains("CodeVerifier is required.", ex.Message);
    }

    [Fact]
    public async Task ExchangeCodeForIdTokenAsync_When_RedirectUriNull_ThrowsArgumentException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["GoogleAuth:DesktopClientId"]).Returns("cid");
        config.Setup(c => c["GoogleAuth:DesktopClientSecret"]).Returns("secret");
        var http = new HttpClient();
        var sut = new GoogleAuthCodeExchangeService(http, config.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ExchangeCodeForIdTokenAsync("code", "verifier", "", CancellationToken.None));

        Assert.Contains("RedirectUri is required.", ex.Message);
    }
}
