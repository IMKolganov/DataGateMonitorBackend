using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

/// <summary>Per-server result from the last availability probe cycle.</summary>
public sealed class AvailabilityCheckServerResultDto
{
    public int VpnServerId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public bool IsAvailableByExternalProbe { get; set; }

    public bool? Reachable { get; set; }

    public string? Summary { get; set; }

    public DateTimeOffset? CheckedAtUtc { get; set; }

    public long? DurationMs { get; set; }

    public string? Error { get; set; }

    public AvailabilityProbeResultDto? Result { get; set; }
}
