using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.TelegramBotUser.Requests;

public class RegisterUserRequest
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    public string? Username { get; set; } = string.Empty;
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
}