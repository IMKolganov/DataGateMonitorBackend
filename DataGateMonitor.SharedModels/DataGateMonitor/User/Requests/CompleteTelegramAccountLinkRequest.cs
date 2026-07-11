using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

/// <summary>
/// Called by the Telegram bot when the user submits a link code from the client app.
/// </summary>
public sealed class CompleteTelegramAccountLinkRequest
{
    [Required]
    [MinLength(4)]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long TelegramId { get; set; }
}
