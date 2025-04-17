using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Requests;

public class GetTelegramUserLanguageRequest
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
}