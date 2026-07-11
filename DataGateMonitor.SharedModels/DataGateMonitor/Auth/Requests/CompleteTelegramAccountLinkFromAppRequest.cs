using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>Logged-in app user submits a link code received from the Telegram bot.</summary>
public sealed class CompleteTelegramAccountLinkFromAppRequest
{
    [Required]
    [MinLength(4)]
    [MaxLength(32)]
    public string Code { get; set; } = default!;
}
