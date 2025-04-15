using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class TelegramBotUser: BaseEntity<int>
{
    [Required]
    public long TelegramId { get; set; }

    public string? Username { get; set; } 

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}