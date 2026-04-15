using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

public class TelegramBotUser: BaseEntity<int>
{
    [Required]
    public long TelegramId { get; set; }
    public string? Username { get; set; } 
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? LanguageCode { get; set; }
    public bool IsAdmin { get; set; } = false;
    public bool IsPremium { get; set; } = false;
    public bool IsBlocked { get; set; } = false;
}