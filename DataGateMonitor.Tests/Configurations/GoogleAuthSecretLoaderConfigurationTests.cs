using Moq;
using DataGateMonitor.Configurations;
using Serilog;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class GoogleAuthSecretLoaderConfigurationTests
{
    [Fact]
    public void LoadSecret_WhenEnvVarSet_ReturnsSecret()
    {
        var logger = new Mock<Serilog.ILogger>().Object;
        var prevEnv = Environment.GetEnvironmentVariable("GOOGLE_AUTH_SECRET");
        try
        {
            Environment.SetEnvironmentVariable("GOOGLE_AUTH_SECRET", "test-google-secret");

            var result = GoogleAuthSecretLoaderConfiguration.LoadSecret(logger);

            Assert.NotNull(result);
            Assert.Equal("test-google-secret", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_AUTH_SECRET", prevEnv);
        }
    }

    [Fact]
    public void LoadSecret_WhenEnvVarNotSetAndNoFile_ReturnsNull()
    {
        var logger = new Mock<Serilog.ILogger>().Object;
        var prevEnv = Environment.GetEnvironmentVariable("GOOGLE_AUTH_SECRET");
        var prevDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        var localPath = Path.GetFullPath("secrets/google-auth-secret.txt");
        try
        {
            Environment.SetEnvironmentVariable("GOOGLE_AUTH_SECRET", null);
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "false");
            if (File.Exists(localPath))
                File.Delete(localPath);

            var result = GoogleAuthSecretLoaderConfiguration.LoadSecret(logger);

            Assert.Null(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_AUTH_SECRET", prevEnv);
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", prevDocker);
        }
    }
}
