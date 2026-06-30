using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;

public class CertExpiryRunSummaryDto
{
    public Guid RunId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public long? DurationMs { get; set; }
    public CertExpiryRunStatus Status { get; set; }
    public int? VpnServerId { get; set; }
    public string ScopeLabel { get; set; } = string.Empty;
    public bool SendNotifications { get; set; }
    public bool IsScheduled { get; set; }
    public int ServersChecked { get; set; }
    public int ProfilesChecked { get; set; }
    public int Expired { get; set; }
    public int ExpiringSoon { get; set; }
    public int MissingOnNode { get; set; }
    public int ServerFailures { get; set; }
    public string? ErrorMessage { get; set; }
}
