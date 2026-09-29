using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;

public sealed class AvailabilityCheckStatusResponse
{
    public bool Enabled { get; set; }

    public string TargetUrl { get; set; } = string.Empty;

    public string ProbeUrl { get; set; } = string.Empty;

    public DateTimeOffset? LastCheckedAtUtc { get; set; }

    public long? LastDurationMs { get; set; }

    public string? LastError { get; set; }

    public AvailabilityProbeResultDto? LastResult { get; set; }
}
