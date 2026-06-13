namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;

public class SentEmailLogDto
{
    public int Id { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public int? RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? SentByUserId { get; set; }
}
