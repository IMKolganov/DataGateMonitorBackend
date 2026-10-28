namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.LocalIssuance.Responses;

public sealed class LocalProfileView
{
    public string CommonName { get; set; } = string.Empty;
    public string Origin { get; set; } = "local";
    public string Status { get; set; } = "unknown";
    public string SerialNumber { get; set; } = string.Empty;
    public string? CertFingerprintSha256 { get; set; }
    public bool IsServiceProfile { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
