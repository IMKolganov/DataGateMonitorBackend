using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Requests;

public class IsExistTelegramUserLanguagePreferenceRequest
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
}