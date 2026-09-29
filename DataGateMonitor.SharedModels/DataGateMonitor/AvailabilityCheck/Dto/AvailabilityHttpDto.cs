namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;

public sealed class AvailabilityHttpDto
{
    public bool Ok { get; set; }

    public string? Url { get; set; }

    public int? StatusCode { get; set; }

    public double? LatencyMs { get; set; }

    public int? BytesRead { get; set; }

    public string? ContentType { get; set; }

    public string? BodyPreview { get; set; }

    public string? Error { get; set; }

    public string? ResponseSummary { get; set; }
}
