using System.Security.Cryptography;
using OpenVPNGateMonitor.Services.Others;

namespace OpenVPNGateMonitor.Tests.Services.Others;

public class RsaKeyGeneratorTests
{
    [Fact]
    public void GenerateAndSaveKeyPair_CreatesFiles_WithValidPem()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        var privPath = Path.Combine(tempDir.FullName, "private.pem");
        var pubPath = Path.Combine(tempDir.FullName, "public.pem");

        try
        {
            RsaKeyGenerator.GenerateAndSaveKeyPair(privPath, pubPath);

            Assert.True(File.Exists(privPath));
            Assert.True(File.Exists(pubPath));

            var privPem = File.ReadAllText(privPath);
            var pubPem = File.ReadAllText(pubPath);

            Assert.Contains("-----BEGIN PRIVATE KEY-----", privPem);
            Assert.Contains("-----END PRIVATE KEY-----", privPem);
            Assert.Contains("-----BEGIN PUBLIC KEY-----", pubPem);
            Assert.Contains("-----END PUBLIC KEY-----", pubPem);

            // Validate that the PEM content can be imported by RSA
            using var rsaFromPrivate = RSA.Create();
            rsaFromPrivate.ImportFromPem(privPem);

            using var rsaFromPublic = RSA.Create();
            rsaFromPublic.ImportFromPem(pubPem);

            // Basic sanity: can we sign and verify with the generated keys?
            var data = RandomNumberGenerator.GetBytes(64);
            var signature = rsaFromPrivate.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var verified = rsaFromPublic.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Assert.True(verified);
        }
        finally
        {
            try { if (File.Exists(privPath)) File.Delete(privPath); } catch { /* ignore */ }
            try { if (File.Exists(pubPath)) File.Delete(pubPath); } catch { /* ignore */ }
            try { tempDir.Delete(true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void GenerateAndSaveKeyPair_PublicMatchesPrivateKey()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        var privPath = Path.Combine(tempDir.FullName, "private.pem");
        var pubPath = Path.Combine(tempDir.FullName, "public.pem");

        try
        {
            RsaKeyGenerator.GenerateAndSaveKeyPair(privPath, pubPath);

            var privPem = File.ReadAllText(privPath);
            var pubPem = File.ReadAllText(pubPath);

            using var rsaFromPrivate = RSA.Create();
            rsaFromPrivate.ImportFromPem(privPem);

            // Export public from private and compare SubjectPublicKeyInfo bytes
            var exportedPublicInfo = rsaFromPrivate.ExportSubjectPublicKeyInfo();

            using var rsaFromPublic = RSA.Create();
            rsaFromPublic.ImportFromPem(pubPem);
            var readPublicInfo = rsaFromPublic.ExportSubjectPublicKeyInfo();

            Assert.Equal(exportedPublicInfo, readPublicInfo);
        }
        finally
        {
            try { if (File.Exists(privPath)) File.Delete(privPath); } catch { /* ignore */ }
            try { if (File.Exists(pubPath)) File.Delete(pubPath); } catch { /* ignore */ }
            try { tempDir.Delete(true); } catch { /* ignore */ }
        }
    }
}
