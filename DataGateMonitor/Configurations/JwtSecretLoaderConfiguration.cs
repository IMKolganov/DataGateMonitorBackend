using System.Security.Cryptography;
using ILogger = Serilog.ILogger;

namespace DataGateMonitor.Configurations;

public static class JwtSecretLoaderConfiguration
{
    private const string DockerSecretPath = "/app/secrets/jwt-secret.txt";
    private const string LocalSecretPath = "secrets/jwt-secret.txt";

    public static string LoadOrGenerateSecret(IConfiguration configuration, ILogger logger)
    {
        var isDocker = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        logger.Information("DOTNET_RUNNING_IN_CONTAINER = {IsDocker}", isDocker);

        var relativePath = isDocker ? DockerSecretPath : LocalSecretPath;
        var fullPath = Path.GetFullPath(relativePath);

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Prefer IConfiguration ("Jwt:Secret" => env Jwt__Secret, appsettings, etc.)
        var jwtSecret =
            configuration["Jwt:Secret"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET"); // legacy fallback

        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            File.WriteAllText(fullPath, jwtSecret);
            logger.Information("JWT secret loaded from configuration/environment and saved to file at: {Path}", fullPath);
            return jwtSecret;
        }

        if (File.Exists(fullPath))
        {
            jwtSecret = File.ReadAllText(fullPath).Trim();
            if (!string.IsNullOrWhiteSpace(jwtSecret))
            {
                logger.Information("JWT secret loaded from file at: {Path}", fullPath);
                return jwtSecret;
            }
        }

        jwtSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        File.WriteAllText(fullPath, jwtSecret);
        logger.Information("JWT secret generated and saved to file at: {Path}", fullPath);

        return jwtSecret;
    }
}