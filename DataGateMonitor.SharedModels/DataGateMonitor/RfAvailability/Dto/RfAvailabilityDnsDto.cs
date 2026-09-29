namespace DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Dto;

public sealed class RfAvailabilityDnsDto
{
    public bool Ok { get; set; }

    public List<string> Addresses { get; set; } = [];

    public double? LatencyMs { get; set; }

    public string? Error { get; set; }
}
