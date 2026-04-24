using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Responses;

public class SetTelegramUserLanguageResponse
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    public Language PreferredLanguage { get; set; }
}