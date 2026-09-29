namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

/// <summary>
/// Wire contract for an external availability probe (e.g. status.rackot.ru <c>check.cgi</c>).
/// Any compatible probe endpoint that returns this JSON shape can be configured.
/// </summary>
public sealed class AvailabilityProbeResultDto
{
    public string Input { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public bool Reachable { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? ProbeFrom { get; set; }

    public AvailabilityDnsDto? Dns { get; set; }

    public List<AvailabilityPortDto> Ports { get; set; } = [];

    public AvailabilityHttpDto? Http { get; set; }
}
