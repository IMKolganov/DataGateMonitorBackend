using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;

public class CertExpiryProfileResultDto
{
    public int IssuedOvpnFileId { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public CertExpiryProfileOutcome Outcome { get; set; }
    public DateTimeOffset? ExpiryUtc { get; set; }
    public int? DaysLeft { get; set; }
    public string? SerialNumber { get; set; }
    public bool PathsMatch { get; set; }
    public bool NotificationSent { get; set; }
}
