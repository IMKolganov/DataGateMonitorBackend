using System.ComponentModel.DataAnnotations;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Responses;

public class SetTelegramUserLanguageResponse
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    public Language PreferredLanguage { get; set; }
}