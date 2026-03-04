using System.ComponentModel.DataAnnotations;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Models;

public class IncomingMessageLog: BaseEntity<int>
{
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
    public DateTimeOffset ReceivedAt { get; set; }
}