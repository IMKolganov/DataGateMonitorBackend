namespace DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Dto;

public sealed class RfAvailabilityPortDto
{
    public int Port { get; set; }

    public bool Ok { get; set; }

    public double? LatencyMs { get; set; }

    public string? Error { get; set; }
}
