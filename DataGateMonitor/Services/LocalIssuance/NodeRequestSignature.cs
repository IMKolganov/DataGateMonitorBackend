using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DataGateMonitor.Services.LocalIssuance;

/// <summary>
/// Node → backend request signature. Must match DataGateOpenVpnManager <c>NodeRequestSignature</c> byte for byte:
/// <c>"DGN1\n{unixSeconds}\n{METHOD}\n{path}\n{hex(sha256(body))}"</c>, ECDSA P-256 / SHA-256, IEEE P1363, base64.
/// </summary>
public static class NodeRequestSignature
{
    public const string KeyIdHeader = "X-DataGate-Node-Key";
    public const string TimestampHeader = "X-DataGate-Timestamp";
    public const string SignatureHeader = "X-DataGate-Signature";

    /// <summary>Clock skew window; replays inside it are harmless because events are idempotent by serial.</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(10);

    public static byte[] CanonicalBytes(long unixSeconds, string method, string path, byte[] body) =>
        Encoding.UTF8.GetBytes(
            $"DGN1\n{unixSeconds}\n{method.ToUpperInvariant()}\n{path}\n{Convert.ToHexStringLower(SHA256.HashData(body))}");

    public static bool Verify(
        string publicKeyPem,
        string? timestampHeader,
        string? signatureHeader,
        string method,
        string path,
        byte[] body,
        DateTimeOffset now,
        out string error)
    {
        if (!long.TryParse(timestampHeader, NumberStyles.None, CultureInfo.InvariantCulture, out var unixSeconds))
        {
            error = "missing or invalid timestamp";
            return false;
        }

        var signedAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if ((now - signedAt).Duration() > MaxClockSkew)
        {
            error = "timestamp outside allowed clock skew";
            return false;
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(signatureHeader ?? string.Empty);
        }
        catch (FormatException)
        {
            error = "signature is not base64";
            return false;
        }

        using var key = ECDsa.Create();
        key.ImportFromPem(publicKeyPem);
        if (!key.VerifyData(CanonicalBytes(unixSeconds, method, path, body), signature, HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
        {
            error = "bad signature";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

public static class NodePublicKey
{
    /// <summary>Accepts an ECDSA P-256 SPKI PEM; returns the normalized PEM and its SPKI SHA-256 fingerprint.</summary>
    public static bool TryParse(string? pem, out string normalizedPem, out string fingerprint)
    {
        normalizedPem = string.Empty;
        fingerprint = string.Empty;
        if (string.IsNullOrWhiteSpace(pem) || pem.Length > 1024)
            return false;

        try
        {
            using var key = ECDsa.Create();
            key.ImportFromPem(pem);
            if (key.KeySize != 256)
                return false;
            var spki = key.ExportSubjectPublicKeyInfo();
            normalizedPem = key.ExportSubjectPublicKeyInfoPem();
            fingerprint = Convert.ToHexStringLower(SHA256.HashData(spki));
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            return false;
        }
    }
}
