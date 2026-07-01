using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;

public class CertExpiryCheckRunResponse
{
    public Guid RunId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public long? DurationMs { get; set; }
    public CertExpiryRunStatus Status { get; set; }
    public int? VpnServerId { get; set; }
    public string ScopeLabel { get; set; } = string.Empty;
    public int WarningDays { get; set; }
    public bool SendNotifications { get; set; }
    public bool IsScheduled { get; set; }
    public CertExpiryCheckSummaryDto Summary { get; set; } = new();
    public List<CertExpiryServerResultDto> Servers { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
