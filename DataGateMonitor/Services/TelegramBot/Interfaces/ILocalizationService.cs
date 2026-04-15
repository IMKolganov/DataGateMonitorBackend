using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.TelegramBot.Interfaces;

public interface ILocalizationService
{
    Task<TelegramUserLanguagePreference> SetTelegramUserLanguageAsync(TelegramUserLanguagePreference request,
        CancellationToken cancellationToken);
    Task<Language> GetTelegramUserLanguageAsync(long telegramId, CancellationToken cancellationToken);
    Task<bool> IsExistTelegramUserLanguagePreferenceAsync(long telegramId, CancellationToken cancellationToken);
    Task<string> GetTextForTelegramUser(string key, long telegramId, 
        CancellationToken cancellationToken, Language? language = null);
}