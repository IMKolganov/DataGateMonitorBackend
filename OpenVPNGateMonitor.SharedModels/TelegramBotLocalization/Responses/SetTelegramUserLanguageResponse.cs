using System.ComponentModel.DataAnnotations;
using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Enums;

namespace OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Responses;

public class SetTelegramUserLanguageResponse
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    public Language PreferredLanguage { get; set; }
}