using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>
/// Dashboard/client user requests a link code. When <see cref="TelegramId"/> is omitted,
/// the code is completed in the Telegram bot (bot supplies the sender id). When set, the code
/// is bound to that specific Telegram account (legacy).
/// </summary>
public sealed class RequestTelegramAccountLinkCodeRequest
{
    /// <summary>Optional. Omit on mobile — user enters the code in the bot instead.</summary>
    [Range(1, long.MaxValue)]
    public long? TelegramId { get; set; }
}
