using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.Configurations;
using Serilog;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class JwtSecretLoaderConfigurationTests
{
    [Fact]
    public void LoadOrGenerateSecret_WhenConfigHasJwtSecret_ReturnsThatSecret()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = "test-secret-from-config" })
            .Build();
        var logger = new Mock<Serilog.ILogger>().Object;

        var result = JwtSecretLoaderConfiguration.LoadOrGenerateSecret(config, logger);

        Assert.Equal("test-secret-from-config", result);
    }

    [Fact]
    public void LoadOrGenerateSecret_WhenConfigMissing_CanGenerateOrReadFromEnv()
    {
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<Serilog.ILogger>().Object;
        var prevEnv = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "false");
            var result = JwtSecretLoaderConfiguration.LoadOrGenerateSecret(config, logger);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", prevEnv);
        }
    }
}
