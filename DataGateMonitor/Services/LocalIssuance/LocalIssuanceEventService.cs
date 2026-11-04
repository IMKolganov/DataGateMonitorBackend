using DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance;
using DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Requests;
using System.Text.RegularExpressions;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataGateMonitor.Services.LocalIssuance;

public interface ILocalIssuanceEventService
{
    /// <summary>Finds the server whose trusted node key signed the request, or null.</summary>
    Task<VpnServer?> AuthenticateAsync(
        string? keyFingerprint, string? timestamp, string? signature, string method, string path, byte[] body,
        CancellationToken ct);

    /// <summary>Idempotent per (server, serial): repeats return Duplicate / AlreadyRevoked.</summary>
    Task<(LocalIssuanceEventOutcome Outcome, int IssuedOvpnFileId)> ApplyAsync(
        VpnServer server, LocalIssuanceEventRequest evt, CancellationToken ct);
}

public sealed partial class LocalIssuanceEventService(
    ApplicationDbContext db,
    TimeProvider timeProvider,
    ILogger<LocalIssuanceEventService> logger) : ILocalIssuanceEventService
{
    public const string SignedPath = "/api/open-vpn-servers/local-issuance-events";
    public const string LocalExternalId = "local";
    private const string Unavailable = "unavailable";

    [GeneratedRegex("^[0-9A-Fa-f]{1,128}$")]
    private static partial Regex SerialPattern();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex FingerprintPattern();

    public async Task<VpnServer?> AuthenticateAsync(
        string? keyFingerprint, string? timestamp, string? signature, string method, string path, byte[] body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyFingerprint) || !FingerprintPattern().IsMatch(keyFingerprint))
            return null;

        var server = await db.VpnServers
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.NodeKeyFingerprint == keyFingerprint && s.NodePublicKeyPem != null, ct);
        if (server is null)
        {
            logger.LogWarning("Local issuance event signed by unknown node key {Fingerprint}", keyFingerprint);
            return null;
        }

        if (!NodeRequestSignature.Verify(server.NodePublicKeyPem!, timestamp, signature, method, path, body,
                timeProvider.GetUtcNow(), out var error))
        {
            logger.LogWarning("Local issuance event for VpnServer {VpnServerId} failed verification: {Error}", server.Id, error);
            return null;
        }

        return server;
    }

    public async Task<(LocalIssuanceEventOutcome Outcome, int IssuedOvpnFileId)> ApplyAsync(
        VpnServer server, LocalIssuanceEventRequest evt, CancellationToken ct)
    {
        Validate(evt);
        var serial = evt.SerialNumber.ToUpperInvariant();
        var existing = await db.IssuedOvpnFiles
            .FirstOrDefaultAsync(f => f.VpnServerId == server.Id && f.CertSerialNumber == serial, ct);

        if (evt.EventType == LocalIssuanceEventTypes.Issued)
        {
            if (existing is not null)
                return (LocalIssuanceEventOutcome.Duplicate, existing.Id);

            var created = NewRow(server, evt, serial, revoked: false);
            if (!await TryInsertAsync(created, ct))
                return (LocalIssuanceEventOutcome.Duplicate, await IdBySerialAsync(server.Id, serial, ct));

            logger.LogInformation(
                "Recorded locally issued {CommonName} (serial {Serial}, service={IsService}) on VpnServer {VpnServerId}",
                created.CommonName, serial, created.IsServiceProfile, server.Id);
            return (LocalIssuanceEventOutcome.Created, created.Id);
        }

        if (existing is null)
        {
            // Revoke reached us first (or the issue was lost before this feature): keep a revoked record.
            var revokedRow = NewRow(server, evt, serial, revoked: true);
            if (!await TryInsertAsync(revokedRow, ct))
                return (LocalIssuanceEventOutcome.AlreadyRevoked, await IdBySerialAsync(server.Id, serial, ct));
            return (LocalIssuanceEventOutcome.Revoked, revokedRow.Id);
        }

        if (existing.IsRevoked)
            return (LocalIssuanceEventOutcome.AlreadyRevoked, existing.Id);

        existing.IsRevoked = true;
        existing.Message = $"Revoked locally on the node at {evt.OccurredAt:O}";
        existing.LastUpdate = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Recorded local revoke of {CommonName} (serial {Serial}) on VpnServer {VpnServerId}",
            existing.CommonName, serial, server.Id);
        return (LocalIssuanceEventOutcome.Revoked, existing.Id);
    }

    private IssuedOvpnFile NewRow(VpnServer server, LocalIssuanceEventRequest evt, string serial, bool revoked)
    {
        var now = timeProvider.GetUtcNow();
        var fileName = Coalesce(evt.FileName, $"{evt.CommonName}.ovpn");
        return new IssuedOvpnFile
        {
            VpnServerId = server.Id,
            ExternalId = LocalExternalId,
            CommonName = evt.CommonName,
            CertId = Unavailable,
            FileName = fileName,
            FilePath = Coalesce(evt.FilePath, fileName),
            IssuedAt = evt.OccurredAt == default ? now : evt.OccurredAt,
            IssuedTo = Coalesce(evt.IssuedTo, evt.IsServiceProfile ? "local-service" : "local-admin"),
            PemFilePath = Unavailable,
            CertFilePath = Coalesce(evt.CertFilePath, Unavailable),
            KeyFilePath = Coalesce(evt.KeyFilePath, Unavailable),
            ReqFilePath = Unavailable,
            IsRevoked = revoked,
            Message = revoked ? $"Revoked locally on the node at {evt.OccurredAt:O}" : "Issued locally on the node",
            Origin = IssuedOvpnFileOrigin.Local,
            IsServiceProfile = evt.IsServiceProfile,
            CertSerialNumber = serial,
            CertFingerprintSha256 = string.IsNullOrWhiteSpace(evt.CertFingerprintSha256) ? null : evt.CertFingerprintSha256,
            CreateDate = now,
            LastUpdate = now,
        };
    }

    /// <summary>
    /// False when a concurrent delivery already inserted the same serial (unique index). Any other write failure
    /// propagates: answering "duplicate" would make the node drop an event that was never stored.
    /// </summary>
    private async Task<bool> TryInsertAsync(IssuedOvpnFile row, CancellationToken ct)
    {
        db.IssuedOvpnFiles.Add(row);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(row).State = EntityState.Detached;
            logger.LogInformation(ex, "Concurrent insert for serial {Serial}; treating as duplicate.", row.CertSerialNumber);
            return false;
        }
    }

    private async Task<int> IdBySerialAsync(int vpnServerId, string serial, CancellationToken ct) =>
        await db.IssuedOvpnFiles.Where(f => f.VpnServerId == vpnServerId && f.CertSerialNumber == serial)
            .Select(f => f.Id).FirstOrDefaultAsync(ct);

    private static void Validate(LocalIssuanceEventRequest evt)
    {
        if (evt.EventType is not (LocalIssuanceEventTypes.Issued or LocalIssuanceEventTypes.Revoked))
            throw new ArgumentException($"Unknown EventType '{evt.EventType}'.");
        if (!string.Equals(evt.Origin, "local", StringComparison.Ordinal))
            throw new ArgumentException("Origin must be 'local'.");
        if (string.IsNullOrWhiteSpace(evt.CommonName) || evt.CommonName.Length > 255)
            throw new ArgumentException("CommonName is required (max 255).");
        if (!SerialPattern().IsMatch(evt.SerialNumber ?? string.Empty))
            throw new ArgumentException("SerialNumber must be hex (max 128).");
        if (!string.IsNullOrWhiteSpace(evt.CertFingerprintSha256) && !FingerprintPattern().IsMatch(evt.CertFingerprintSha256))
            throw new ArgumentException("CertFingerprintSha256 must be 64 lower-case hex chars.");
        if ((evt.FileName?.Length ?? 0) > 255 || (evt.FilePath?.Length ?? 0) > 500
            || (evt.CertFilePath?.Length ?? 0) > 500 || (evt.KeyFilePath?.Length ?? 0) > 500 || (evt.IssuedTo?.Length ?? 0) > 255)
            throw new ArgumentException("A path or name field is too long.");
    }

    private static string Coalesce(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
