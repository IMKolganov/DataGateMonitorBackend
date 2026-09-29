using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;

public sealed class AvailabilityCheckStatusResponse
{
    public bool Enabled { get; set; }

    public string ProbeUrl { get; set; } = string.Empty;

    /// <summary>Background probe interval in seconds.</summary>
    public int IntervalSeconds { get; set; } = 300;

    public DateTimeOffset? LastCheckedAtUtc { get; set; }

    public List<AvailabilityCheckServerResultDto> Servers { get; set; } = [];
}
