namespace DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Requests;

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
