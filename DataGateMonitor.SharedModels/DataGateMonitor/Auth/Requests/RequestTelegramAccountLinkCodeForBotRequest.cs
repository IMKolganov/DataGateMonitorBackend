using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>Telegram bot requests a link code for the sender; user enters it in the mobile/desktop app.</summary>
public sealed class RequestTelegramAccountLinkCodeForBotRequest
{
    [Range(1, long.MaxValue)]
    public long TelegramId { get; set; }
}
