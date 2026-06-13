namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;

/// <summary>List row without full HTML body.</summary>
public class EmailBroadcastTemplateSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
}
