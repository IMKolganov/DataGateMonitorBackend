using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>
/// Dashboard/client user requests a link code bound to a specific Telegram account.
/// </summary>
public sealed class RequestTelegramAccountLinkCodeRequest
{
    [Range(1, long.MaxValue)]
    public long TelegramId { get; set; }
}
