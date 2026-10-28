using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;

namespace DataGateMonitor.Services.LocalIssuance;

// Contracts for node-local issuance live in the backend (not SharedModels) so they ship without a NuGet release;
// nodes post plain JSON. Field names match DataGateOpenVpnManager LocalIssuanceEvent.

/// <summary>Discovery announce plus the node's event-signing public key (older nodes simply omit it).</summary>
public sealed class AnnounceVpnServerWithNodeKeyRequest : AnnounceVpnServerRequest
{
    public string? NodePublicKeyPem { get; set; }
}

public static class LocalIssuanceEventTypes
{
    public const string Issued = "issued";
    public const string Revoked = "revoked";
}

public sealed class LocalIssuanceEventRequest
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string CommonName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string? CertFingerprintSha256 { get; set; }
    public bool IsServiceProfile { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? IssuedTo { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? CertFilePath { get; set; }
    public string? KeyFilePath { get; set; }
}

public enum LocalIssuanceEventOutcome
{
    Created,
    Duplicate,
    Revoked,
    AlreadyRevoked
}

public sealed class LocalIssuanceEventResponse
{
    public string Outcome { get; set; } = string.Empty;
    public int IssuedOvpnFileId { get; set; }
}

/// <summary>Per-row extras for the dashboard list (the SharedModels IssuedOvpnFileDto has no such fields yet).</summary>
public sealed class IssuedOvpnFileOriginDto
{
    public int IssuedOvpnFileId { get; set; }
    public string Origin { get; set; } = "dashboard";
    public bool IsServiceProfile { get; set; }
    public string? CertSerialNumber { get; set; }
    public string? CertFingerprintSha256 { get; set; }
}

public sealed class NodeKeyStatusResponse
{
    public string? TrustedFingerprint { get; set; }
    public string? PendingFingerprint { get; set; }
}

public sealed class TrustNodeKeyRequest
{
    /// <summary>Must equal the pending fingerprint (compare with <c>datagate-profile node-id</c> on the node).</summary>
    public string Fingerprint { get; set; } = string.Empty;
}
