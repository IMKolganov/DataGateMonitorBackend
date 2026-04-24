using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Responses;

public class GetTelegramUserLanguageResponse
{
    public Language PreferredLanguage { get; set; }
}