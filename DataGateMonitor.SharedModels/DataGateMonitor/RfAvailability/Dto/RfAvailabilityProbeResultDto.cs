namespace DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Dto;

/// <summary>
/// Probe payload returned by status.rackot.ru <c>check.cgi</c>.
/// </summary>
public sealed class RfAvailabilityProbeResultDto
{
    public string Input { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public bool Reachable { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? ProbeFrom { get; set; }

    public RfAvailabilityDnsDto? Dns { get; set; }

    public List<RfAvailabilityPortDto> Ports { get; set; } = [];

    public RfAvailabilityHttpDto? Http { get; set; }
}
