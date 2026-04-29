using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.TelegramBotUserProfilePhotoTable;

public interface ITelegramBotUserProfilePhotoQueryService
{
    Task<TelegramBotUserProfilePhoto?> GetByTelegramBotUserIdNoTrackingAsync(int telegramBotUserId,
        CancellationToken cancellationToken);

    Task<TelegramBotUserProfilePhoto?> GetByTelegramBotUserIdForUpdateAsync(int telegramBotUserId,
        CancellationToken cancellationToken);

    Task<TelegramBotUserProfilePhotoIdMeta?> GetIdAndFileUniqueIdNoTrackingAsync(int telegramBotUserId,
        CancellationToken cancellationToken);

    /// <summary>Mime, timestamps, file_unique_id — does not load <c>ImageBytes</c>.</summary>
    Task<TelegramBotUserProfilePhotoSummary?> GetSummaryNoTrackingAsync(int telegramBotUserId,
        CancellationToken cancellationToken);
}
