using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.Services.Api.Auth.Registers;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Registers;

public class GoogleTokenValidatorTests
{
    [Fact]
    public async Task ValidateAsync_When_IdTokenEmpty_ThrowsArgumentException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["GoogleAuth:ClientId"]).Returns("web");
        config.Setup(c => c["GoogleAuth:DesktopClientId"]).Returns("desktop");
        config.Setup(c => c["GoogleAuth:IosClientId"]).Returns("ios");

        var sut = new GoogleTokenValidator(config.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ValidateAsync("", CancellationToken.None));

        Assert.Contains("IdToken is required.", ex.Message);
    }

    [Fact]
    public void Constructor_When_ClientIdMissing_ThrowsInvalidOperationException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["GoogleAuth:ClientId"]).Returns((string?)null);
        config.Setup(c => c["GoogleAuth:DesktopClientId"]).Returns("d");
        config.Setup(c => c["GoogleAuth:IosClientId"]).Returns("i");

        var ex = Assert.Throws<InvalidOperationException>(
            () => new GoogleTokenValidator(config.Object));

        Assert.Contains("GoogleAuth:ClientId", ex.Message);
    }
}
