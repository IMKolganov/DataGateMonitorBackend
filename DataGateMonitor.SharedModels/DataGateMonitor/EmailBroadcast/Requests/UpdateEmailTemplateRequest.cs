namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;

public class UpdateEmailTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
}
