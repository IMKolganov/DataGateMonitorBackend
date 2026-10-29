using System.Security.Cryptography;
using System.Text;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.Services.LocalIssuance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataGateMonitor.Tests.Services.LocalIssuance;

/// <summary>Plays the node side: ECDSA P-256 key and the DGN1 request signature.</summary>
internal sealed class FakeNode : IDisposable
{
    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public string PublicKeyPem => _key.ExportSubjectPublicKeyInfoPem();
    public string Fingerprint => Convert.ToHexStringLower(SHA256.HashData(_key.ExportSubjectPublicKeyInfo()));

    public (string Timestamp, string Signature) Sign(byte[] body, DateTimeOffset at, string path = LocalIssuanceEventService.SignedPath)
    {
        var ts = at.ToUnixTimeSeconds();
        var signature = _key.SignData(NodeRequestSignature.CanonicalBytes(ts, "POST", path, body), HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return (ts.ToString(), Convert.ToBase64String(signature));
    }

    /// <summary>Same JSON shape as DataGateOpenVpnManager (Newtonsoft, PascalCase).</summary>
    public static byte[] EventBody(string type = "issued", string serial = "5E71A1", string cn = "db1", string? staticIp = "10.50.28.201") =>
        Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            EventId = Guid.NewGuid(),
            EventType = type,
            Origin = "local",
            CommonName = cn,
            SerialNumber = serial,
            CertFingerprintSha256 = new string('f', 64),
            IsServiceProfile = true,
            StaticIp = staticIp,
            OccurredAt = DateTimeOffset.UtcNow,
            IssuedTo = "local-service",
            FileName = $"{cn}.ovpn",
            FilePath = $"/openvpn-udp/easy-rsa/pki/ovpn_files/{cn}.ovpn",
            CertFilePath = $"/openvpn-udp/easy-rsa/pki/issued/{cn}.crt",
            KeyFilePath = $"/openvpn-udp/easy-rsa/pki/private/{cn}.key",
        }));

    public void Dispose() => _key.Dispose();
}

internal static class LocalIssuanceDb
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataBaseSettings:DefaultSchema"] = "test_schema" })
            .Build();
        return new ApplicationDbContext(options, configuration);
    }

    public static VpnServer AddServer(ApplicationDbContext db, int id, FakeNode? trustedNode = null)
    {
        var now = DateTimeOffset.UtcNow;
        var server = new VpnServer
        {
            Id = id,
            ServerName = $"node-{id}",
            ApiUrl = $"http://10.0.0.{id}:5010/",
            NodePublicKeyPem = trustedNode?.PublicKeyPem,
            NodeKeyFingerprint = trustedNode?.Fingerprint,
            CreateDate = now,
            LastUpdate = now,
        };
        db.VpnServers.Add(server);
        db.SaveChanges();
        return server;
    }
}
