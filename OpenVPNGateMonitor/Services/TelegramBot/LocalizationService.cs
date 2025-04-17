using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Enums;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class LocalizationService(ILogger<ICertVpnService> logger, IUnitOfWork unitOfWork) : ILocalizationService
{
    public async Task<TelegramUserLanguagePreference> SetTelegramUserLanguageAsync(
        TelegramUserLanguagePreference request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Attempting to set language for TelegramId: " +
                              $"{request.TelegramId} to {request.PreferredLanguage}.");

        var userLanguagePreferenceRepository = unitOfWork.GetRepository<TelegramUserLanguagePreference>();
        var userPreference = await userLanguagePreferenceRepository.Query
            .FirstOrDefaultAsync(x => x.TelegramId == request.TelegramId, 
                cancellationToken: cancellationToken);
        
        if (userPreference == null)
        {
            logger.LogInformation($"No existing language preference found for TelegramId: {request.TelegramId}. " +
                                  "Creating a new record.");
        
            userPreference = request.Adapt<TelegramUserLanguagePreference>();
            await userLanguagePreferenceRepository.AddAsync(userPreference, cancellationToken);
        
            logger.LogInformation($"New language preference created for TelegramId: {userPreference.TelegramId} " +
                                  $"with language: {userPreference.PreferredLanguage}.");
        }
        else
        {
            logger.LogInformation($"Existing language preference found for TelegramId: {request.TelegramId}. " +
                                  $"Updating language to: {request.PreferredLanguage}.");
        
            userPreference.PreferredLanguage = request.PreferredLanguage;
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation($"Language preference saved for TelegramId: {request.TelegramId}.");
        
        userPreference = await userLanguagePreferenceRepository.Query
            .FirstOrDefaultAsync(x => x.TelegramId == request.TelegramId, 
                cancellationToken: cancellationToken);

        return userPreference ?? throw new InvalidOperationException($"Language preference not found for TelegramId: " +
                                                                     $"{request.TelegramId}.");
    }

    public async Task<Language> GetTelegramUserLanguageAsync(long telegramId, CancellationToken cancellationToken)
    {
        var userPreference = await unitOfWork.GetQuery<TelegramUserLanguagePreference>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.TelegramId == telegramId, 
                cancellationToken: cancellationToken);

        return userPreference?.PreferredLanguage ?? Language.English;
    }

    public async Task<bool> IsExistTelegramUserLanguagePreferenceAsync(long telegramId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking database for TelegramId: {TelegramId}.", telegramId);

        var userLanguagePreference = await unitOfWork.GetQuery<TelegramUserLanguagePreference>()
            .AsQueryable().AnyAsync(x => x.TelegramId == telegramId,  cancellationToken);

        logger.LogInformation($"Database check for TelegramId {telegramId}: {userLanguagePreference}");

        return userLanguagePreference;
    }

    public async Task<string> GetTextForTelegramUser(string key, long telegramId, CancellationToken cancellationToken, 
        Language? language = null)
    {
        if (language == null)
        {
            var telegramUserLanguagePreference = await unitOfWork.GetQuery<TelegramUserLanguagePreference>()
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.TelegramId == telegramId, 
                    cancellationToken: cancellationToken);
            language = telegramUserLanguagePreference?.PreferredLanguage ?? Language.English;
        }

        var text = await unitOfWork.GetQuery<LocalizationText>()
            .AsQueryable()
            .Where(x => x.Key == key && x.Language == language)
            .Select(x => x.Text)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return text ?? $"[Translation missing for key: {key}, language: {language}]";
    }
}