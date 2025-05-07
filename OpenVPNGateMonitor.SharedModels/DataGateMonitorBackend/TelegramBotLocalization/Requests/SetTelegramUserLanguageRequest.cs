using System.ComponentModel.DataAnnotations;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Requests;

public class SetTelegramUserLanguageRequest
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    public Language PreferredLanguage { get; set; }
}