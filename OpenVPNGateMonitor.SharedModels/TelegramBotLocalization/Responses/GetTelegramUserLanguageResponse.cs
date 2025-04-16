using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Enums;

namespace OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Responses;

public class GetTelegramUserLanguageResponse
{
    public Language PreferredLanguage { get; set; }
}