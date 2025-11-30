using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.LocalizationTextTable;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class LocalizationService(ILogger<LocalizationService> logger,
    ITelegramUserLanguagePreferenceQueryService telegramUserLanguagePreferenceQueryService,
    ICommandService<TelegramUserLanguagePreference, int> telegramUserLanguagePreferenceCommandService,

    ILocalizationTextQueryService localizationTextQueryService) : ILocalizationService
{
    public async Task<TelegramUserLanguagePreference> SetTelegramUserLanguageAsync(
        TelegramUserLanguagePreference request, CancellationToken ct)
    {
        logger.LogInformation($"Attempting to set language for TelegramId: " +
                              $"{request.TelegramId} to {request.PreferredLanguage}.");

        var userPreference = await telegramUserLanguagePreferenceQueryService.GetByTelegramId(request.TelegramId, ct);
        
        if (userPreference == null)
        {
            logger.LogInformation($"No existing language preference found for TelegramId: {request.TelegramId}. " +
                                  "Creating a new record.");
        
            userPreference = request.Adapt<TelegramUserLanguagePreference>();
            logger.LogInformation($"New language preference created for TelegramId: {userPreference.TelegramId} " +
                                  $"with language: {userPreference.PreferredLanguage}.");
            await telegramUserLanguagePreferenceCommandService.AddAsync(userPreference, true, ct);
        

        }
        else
        {
            logger.LogInformation($"Existing language preference found for TelegramId: {request.TelegramId}. " +
                                  $"Updating language to: {request.PreferredLanguage}.");
        
            userPreference.PreferredLanguage = request.PreferredLanguage;
            await telegramUserLanguagePreferenceCommandService.UpdateAsync(userPreference, true, ct);

        }
        
        logger.LogInformation($"Language preference saved for TelegramId: {request.TelegramId}.");
        
        userPreference = await telegramUserLanguagePreferenceQueryService.GetByTelegramId(request.TelegramId, ct);

        return userPreference ?? throw new InvalidOperationException($"Language preference not found for TelegramId: " +
                                                                     $"{request.TelegramId}.");
    }

    public async Task<Language> GetTelegramUserLanguageAsync(long telegramId, CancellationToken ct)
    {
        var userPreference = await telegramUserLanguagePreferenceQueryService.GetByTelegramId(telegramId, ct);

        return userPreference?.PreferredLanguage ?? Language.English;
    }

    public async Task<bool> IsExistTelegramUserLanguagePreferenceAsync(long telegramId, CancellationToken ct)
    {
        logger.LogInformation("Checking database for TelegramId: {TelegramId}.", telegramId);

        var userLanguagePreference = 
            await telegramUserLanguagePreferenceQueryService.AnyByTelegramId(telegramId, ct);
        logger.LogInformation($"Database check for TelegramId {telegramId}: {userLanguagePreference}");

        return userLanguagePreference;
    }

    public async Task<string> GetTextForTelegramUser(string key, long telegramId, CancellationToken ct, 
        Language? language = null)
    {
        if (language == null)
        {
            var telegramUserLanguagePreference = 
                await telegramUserLanguagePreferenceQueryService.GetByTelegramId(telegramId, ct);
            
            language = telegramUserLanguagePreference?.PreferredLanguage ?? Language.English;
        }

        var text = await localizationTextQueryService.GetTextValueByKeyAndLanguageAsync(key, (Language)language, ct);

        return text ?? $"[Translation missing for key: {key}, language: {language}]";
    }
}