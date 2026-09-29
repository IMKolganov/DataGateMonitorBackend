using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

/// <summary>Per-server result from the last availability probe cycle.</summary>
public sealed class AvailabilityCheckServerResultDto
{
    public int VpnServerId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether this server is included in the external availability probe cycle.
    /// Independent of the global AvailabilityCheck kill-switch.
    /// </summary>
    public bool IsAvailabilityCheckEnabled { get; set; } = true;

    public bool IsAvailableByExternalProbe { get; set; }

    public bool? Reachable { get; set; }

    public string? Summary { get; set; }

    public DateTimeOffset? CheckedAtUtc { get; set; }

    public long? DurationMs { get; set; }

    public string? Error { get; set; }

    public AvailabilityProbeResultDto? Result { get; set; }
}
