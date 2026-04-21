using System.Security.Cryptography;
using System.Text;
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
            EnsureJwtSecretKeyLength(jwtSecret, logger);
            File.WriteAllText(fullPath, jwtSecret);
            logger.Information("JWT secret loaded from configuration/environment and saved to file at: {Path}", fullPath);
            return jwtSecret;
        }

        if (File.Exists(fullPath))
        {
            jwtSecret = File.ReadAllText(fullPath).Trim();
            if (!string.IsNullOrWhiteSpace(jwtSecret))
            {
                EnsureJwtSecretKeyLength(jwtSecret, logger);
                logger.Information("JWT secret loaded from file at: {Path}", fullPath);
                return jwtSecret;
            }
        }

        jwtSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        File.WriteAllText(fullPath, jwtSecret);
        logger.Information("JWT secret generated and saved to file at: {Path}", fullPath);

        return jwtSecret;
    }

    /// <summary>HS256 requires signing material ≥ 128 bits; <see cref="SymmetricSecurityKey"/> uses UTF-8 byte length.</summary>
    private static void EnsureJwtSecretKeyLength(string jwtSecret, ILogger logger)
    {
        const int minBytes = 128 / 8;
        var byteLen = Encoding.UTF8.GetByteCount(jwtSecret);
        if (byteLen < minBytes)
        {
            logger.Error(
                "Jwt:Secret / JWT_SECRET is too short for HS256: {ByteLen} UTF-8 bytes (need at least {MinBytes}).",
                byteLen,
                minBytes);
            throw new InvalidOperationException(
                $"JWT signing secret must be at least {minBytes} bytes in UTF-8 (HS256 / 128-bit minimum). " +
                $"Current length is {byteLen} bytes. Set Jwt__Secret or JWT_SECRET to a longer value (e.g. 32+ random characters).");
        }
    }
}