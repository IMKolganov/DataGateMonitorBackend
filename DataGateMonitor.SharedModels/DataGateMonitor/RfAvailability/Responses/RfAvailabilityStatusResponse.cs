using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Responses;

public sealed class RfAvailabilityStatusResponse
{
    public bool Enabled { get; set; }

    public string TargetUrl { get; set; } = string.Empty;

    public string ProbeBaseUrl { get; set; } = string.Empty;

    public DateTimeOffset? LastCheckedAtUtc { get; set; }

    public long? LastDurationMs { get; set; }

    public string? LastError { get; set; }

    public RfAvailabilityProbeResultDto? LastResult { get; set; }
}
