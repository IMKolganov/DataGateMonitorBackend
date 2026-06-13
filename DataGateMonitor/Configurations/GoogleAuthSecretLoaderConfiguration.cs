using ILogger = Serilog.ILogger;

namespace DataGateMonitor.Configurations;

public static class GoogleAuthSecretLoaderConfiguration
{
    private const string DockerSecretPath = "/app/secrets/google-auth-secret.txt";
    private const string LocalSecretPath = "secrets/google-auth-secret.txt";

    public static string? LoadSecret(ILogger logger)
    {
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        logger.Information("DOTNET_RUNNING_IN_CONTAINER = {IsDocker}", isDocker);

        var relativePath = isDocker ? DockerSecretPath : LocalSecretPath;
        var fullPath = Path.GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var envSecret = Environment.GetEnvironmentVariable("GOOGLE_AUTH_SECRET");
        if (!string.IsNullOrEmpty(envSecret))
        {
            File.WriteAllText(fullPath, envSecret);
            logger.Information("GoogleAuth secret loaded from environment and saved to file: {Path}", fullPath);
            return envSecret;
        }

        if (File.Exists(fullPath))
        {
            var secretFromFile = File.ReadAllText(fullPath);
            logger.Information("GoogleAuth secret loaded from file: {Path}", fullPath);
            return secretFromFile;
        }

        logger.Warning("GoogleAuth secret not found. Using only Google ClientId/ClientSecret.");
        return null;
    }
}
