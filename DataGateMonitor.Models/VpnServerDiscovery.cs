using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Models;

/// <summary>
/// Pending self-announce from an OpenVPN/Xray node before an admin adds it as a <see cref="VpnServer"/>.
/// </summary>
public class VpnServerDiscovery : BaseEntity<int>
{
    public VpnServerType ServerType { get; set; } = VpnServerType.OpenVpn;

    [Required, MaxLength(512)]
    public string ApiUrl { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? SuggestedName { get; set; }

    [MaxLength(64)]
    public string? PublicIp { get; set; }

    [MaxLength(64)]
    public string? Version { get; set; }

    public bool IsEnableWss { get; set; }

    public VpnServerDiscoveryStatus Status { get; set; } = VpnServerDiscoveryStatus.Pending;

    public DateTimeOffset LastSeenUtc { get; set; }

    public DateTimeOffset? LastNotifiedUtc { get; set; }

    public int? ResolvedVpnServerId { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    [MaxLength(512)]
    public string? RejectReason { get; set; }

    /// <summary>Node event-signing key from the first announce; copied to the server on approve.</summary>
    public string? NodePublicKeyPem { get; set; }

    [MaxLength(64)]
    public string? NodeKeyFingerprint { get; set; }
}
