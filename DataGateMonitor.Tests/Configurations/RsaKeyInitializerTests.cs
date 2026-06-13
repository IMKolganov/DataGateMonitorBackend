using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Configurations;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class RsaKeyInitializerTests
{
    [Fact]
    public void EnsureRsaKeysExist_DoesNotThrow_WithMinimalConfig()
    {
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger>().Object;

        var exception = Record.Exception(() => RsaKeyInitializer.EnsureRsaKeysExist(config, logger));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureRsaKeysExist_WhenCustomPathsInConfig_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicroserviceJwt:PrivateKeyPath"] = "resources/certs/private.key",
                ["MicroserviceJwt:PublicKeyPath"] = "resources/certs/public.key"
            })
            .Build();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger>().Object;

        var exception = Record.Exception(() => RsaKeyInitializer.EnsureRsaKeysExist(config, logger));

        Assert.Null(exception);
    }
}
