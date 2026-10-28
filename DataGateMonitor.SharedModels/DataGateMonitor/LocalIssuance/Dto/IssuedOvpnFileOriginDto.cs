namespace DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Dto;

public sealed class IssuedOvpnFileOriginDto
{
    public int IssuedOvpnFileId { get; set; }
    public string Origin { get; set; } = "dashboard";
    public bool IsServiceProfile { get; set; }
    public string? CertSerialNumber { get; set; }
    public string? CertFingerprintSha256 { get; set; }
}
