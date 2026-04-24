using DataGateMonitor.Services.Others;

namespace DataGateMonitor.Configurations;

public static class RsaKeyInitializer
{
    public static void EnsureRsaKeysExist(IConfiguration config, ILogger logger)
    {
        var basePath = AppContext.BaseDirectory;
        var certsDir = Path.Combine(basePath, "resources", "certs");

        Directory.CreateDirectory(certsDir);

        var privateKeyConfigPath = config["MicroserviceJwt:PrivateKeyPath"]
                                   ?? Path.Combine("resources", "certs", "private-microservice.key");
        var publicKeyConfigPath = config["MicroserviceJwt:PublicKeyPath"]
                                  ?? Path.Combine("resources", "certs", "public-microservice.key");

        var privateKeyPath = Path.GetFullPath(Path.Combine(basePath, privateKeyConfigPath));
        var publicKeyPath = Path.GetFullPath(Path.Combine(basePath, publicKeyConfigPath));

        if (!File.Exists(privateKeyPath) || !File.Exists(publicKeyPath))
        {
            RsaKeyGenerator.GenerateAndSaveKeyPair(privateKeyPath, publicKeyPath);
            logger.LogInformation("Generated new RSA key pair at startup: {PrivateKey}, {PublicKey}", privateKeyPath, publicKeyPath);
        }
        else
        {
            logger.LogInformation("RSA key pair already exists: {PrivateKey}, {PublicKey}", privateKeyPath, publicKeyPath);
        }
    }
}