using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>
/// Persisted record of a client-facing export for a <c>VpnServer</c> (issue, revoke, download, quotas).
/// Despite the name, this is <b>not</b> OpenVPN-only: for Xray (VLESS) servers the artifact is typically a
/// client link file from DataGateXRayManager; certificate-related paths may still be set by the sidecar.
/// </summary>
/// <remarks>
/// Conceptual name for new code and future API v2: &quot;issued client export&quot; (stack-agnostic).
/// Renaming the entity or <c>/api/open-vpn-files</c> is a breaking change and should ship only with a versioned
/// HTTP API and migration plan for all consumers (dashboard, bots, mobile).
/// </remarks>
public class IssuedOvpnFile : BaseEntity<int>
{
    [Required]
    public int VpnServerId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    [Required]
    public string CommonName { get; set; } = null!;
    public string? CertId { get; set; } = string.Empty;
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateTimeOffset IssuedAt { get; set; }
    public string IssuedTo { get; set; } = null!;
    [Required]
    public string PemFilePath { get; set; } = null!;
    [Required]
    public string CertFilePath { get; set; } = null!;
    [Required]
    public string KeyFilePath { get; set; } = null!;
    [Required]
    public string ReqFilePath { get; set; } = null!;
    [Required]
    public bool IsRevoked { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}