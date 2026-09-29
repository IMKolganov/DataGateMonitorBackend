namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

public sealed class AvailabilityPortDto
{
    public int Port { get; set; }

    public bool Ok { get; set; }

    public double? LatencyMs { get; set; }

    public string? Error { get; set; }
}
