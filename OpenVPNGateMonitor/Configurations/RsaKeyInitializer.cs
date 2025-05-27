using OpenVPNGateMonitor.Services.Others;

namespace OpenVPNGateMonitor.Configurations;

public static class RsaKeyInitializer
{
    public static void EnsureRsaKeysExist(IConfiguration config, ILogger logger)
    {
        var privateKeyPath = config["Jwt:PrivateKeyPath"] ?? "private-microservice.key";
        var publicKeyPath = config["Jwt:PublicKeyPath"] ?? "public-microservice.key";

        if (!File.Exists(privateKeyPath) || !File.Exists(publicKeyPath))
        {
            RsaKeyGenerator.GenerateAndSaveKeyPair(privateKeyPath, publicKeyPath);
            logger.LogInformation("Generated new RSA key pair at startup.");
        }
        else
        {
            logger.LogInformation("RSA key pair already exists.");
        }
    }
}
