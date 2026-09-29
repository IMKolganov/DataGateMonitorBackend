namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

public sealed class AvailabilityDnsDto
{
    public bool Ok { get; set; }

    public List<string> Addresses { get; set; } = [];

    public double? LatencyMs { get; set; }

    public string? Error { get; set; }
}
