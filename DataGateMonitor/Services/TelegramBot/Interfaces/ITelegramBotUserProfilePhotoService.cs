using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;

namespace DataGateMonitor.Services.TelegramBot.Interfaces;

public interface ITelegramBotUserProfilePhotoService
{
    Task<UpsertTelegramBotUserProfilePhotoResponse> UpsertAsync(
        UpsertTelegramBotUserProfilePhotoRequest request,
        CancellationToken cancellationToken);

    Task<TelegramBotUserProfilePhotoMetaResponse?> GetMetaByTelegramIdAsync(long telegramId,
        CancellationToken cancellationToken);

    /// <summary>Returns stored image or null if none.</summary>
    Task<(byte[] Bytes, string MimeType)?> GetImageByTelegramIdAsync(long telegramId,
        CancellationToken cancellationToken);

    Task<TelegramBotUserProfilePhotoIndexResponse> GetPhotoIndexAsync(CancellationToken cancellationToken);

    Task ApplyHasProfilePhotoFlagsAsync(IReadOnlyList<TelegramBotUserDto> users, CancellationToken cancellationToken);
}
