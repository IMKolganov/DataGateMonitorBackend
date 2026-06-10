using Microsoft.Extensions.Configuration;
using DataGateMonitor.Services.Api.Auth.Registers;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Registers;

public class MicroserviceTokenServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsNonEmptyJwt()
    {
        var baseDir = AppContext.BaseDirectory;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicroserviceJwt:PrivateKeyPath"] = Path.Combine("resources", "certs", "private-microservice.key"),
                ["MicroserviceJwt:PublicKeyPath"] = Path.Combine("resources", "certs", "public-microservice.key"),
            })
            .Build();

        var sut = new MicroserviceTokenService(config);

        var token = sut.GenerateToken("subject", "purpose", "backend", "DataGateOpenVpnManager");

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(sut.ValidateToken(token, out var principal));
        Assert.NotNull(principal);
    }

    [Fact]
    public void GetPublicKeyPem_ReturnsPemContent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicroserviceJwt:PrivateKeyPath"] = Path.Combine("resources", "certs", "private-microservice.key"),
                ["MicroserviceJwt:PublicKeyPath"] = Path.Combine("resources", "certs", "public-microservice.key"),
            })
            .Build();

        var sut = new MicroserviceTokenService(config);

        var pem = sut.GetPublicKeyPem();

        Assert.Contains("BEGIN", pem);
        Assert.Contains("KEY", pem);
    }
}
