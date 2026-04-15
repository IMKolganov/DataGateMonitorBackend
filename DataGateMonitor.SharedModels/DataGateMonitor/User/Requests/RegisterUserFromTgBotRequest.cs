using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

public class RegisterUserFromTgBotRequest
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    public string? Username { get; set; } = string.Empty;
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
    public string? LanguageCode { get; set; } = string.Empty;
    public bool? IsPremium { get; set; } = false;
}