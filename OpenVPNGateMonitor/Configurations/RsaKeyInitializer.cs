using OpenVPNGateMonitor.Services.Others;

namespace OpenVPNGateMonitor.Configurations;

public static class RsaKeyInitializer
{
    public static void EnsureRsaKeysExist(IConfiguration config, ILogger logger)
    {
        var basePath = AppContext.BaseDirectory;
        var certsDir = Path.Combine(basePath, "resources", "certs");

        if (!Directory.Exists(certsDir))
            Directory.CreateDirectory(certsDir);

        var privateKeyPath = config["MicroserviceJwt:PrivateKeyPath"]
                             ?? Path.Combine(certsDir, "private-microservice.key");
        var publicKeyPath = config["MicroserviceJwt:PublicKeyPath"]
                            ?? Path.Combine(certsDir, "public-microservice.key");

        if (!File.Exists(privateKeyPath) || !File.Exists(publicKeyPath))
        {
            RsaKeyGenerator.GenerateAndSaveKeyPair(privateKeyPath, publicKeyPath);
            logger.LogInformation("Generated new RSA key pair at startup in {Path}", certsDir);
        }
        else
        {
            logger.LogInformation("RSA key pair already exists in {Path}", certsDir);
        }
    }
}