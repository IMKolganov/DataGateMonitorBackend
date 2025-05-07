using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Responses;

public class GetTelegramUserLanguageResponse
{
    public Language PreferredLanguage { get; set; }
}