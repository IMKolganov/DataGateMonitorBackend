using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;

public class MessageDto
{
    public int Id { get; set; }
    [Required]
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string MessageText { get; set; } = null!;
    public string? FileType { get; set; }
    public string? FileId { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? FilePath { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}