namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;

public class EmailBroadcastTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public int? CreatedByUserId { get; set; }
}
