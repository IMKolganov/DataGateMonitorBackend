using System.Security.Cryptography;

namespace OpenVPNGateMonitor.Services.Others;

public static class RsaKeyGenerator
{
    public static void GenerateAndSaveKeyPair(string privateKeyPath, string publicKeyPath)
    {
        using var rsa = RSA.Create(2048);

        var privateKeyPem = ExportPrivateKeyToPem(rsa);
        File.WriteAllText(privateKeyPath, privateKeyPem);

        var publicKeyPem = ExportPublicKeyToPem(rsa);
        File.WriteAllText(publicKeyPath, publicKeyPem);
    }

    private static string ExportPrivateKeyToPem(RSA rsa)
    {
        var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        var base64 = Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks);
        return $"-----BEGIN PRIVATE KEY-----\n{base64}\n-----END PRIVATE KEY-----";
    }

    private static string ExportPublicKeyToPem(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var base64 = Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks);
        return $"-----BEGIN PUBLIC KEY-----\n{base64}\n-----END PUBLIC KEY-----";
    }
}