using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.TelegramBotUserProfilePhotoTable;
using DataGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;

namespace DataGateMonitor.Services.TelegramBot;

public sealed class TelegramBotUserProfilePhotoService(
    ITelegramBotUserQueryService telegramBotUserQueryService,
    ITelegramBotUserProfilePhotoQueryService profilePhotoQuery,
    ICommandService<TelegramBotUserProfilePhoto, int> photoCommand,
    ILogger<TelegramBotUserProfilePhotoService> logger) : ITelegramBotUserProfilePhotoService
{
    private const int MaxDecodedPhotoBytes = 5 * 1024 * 1024;

    public async Task<UpsertTelegramBotUserProfilePhotoResponse> UpsertAsync(
        UpsertTelegramBotUserProfilePhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProfilePhotoBase64))
            return new UpsertTelegramBotUserProfilePhotoResponse { Updated = false };

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.ProfilePhotoBase64);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid base64 profile photo for TelegramId {TelegramId}", request.TelegramId);
            return new UpsertTelegramBotUserProfilePhotoResponse { Updated = false };
        }

        if (bytes.Length == 0 || bytes.Length > MaxDecodedPhotoBytes)
        {
            logger.LogWarning("Profile photo size invalid for TelegramId {TelegramId}: {Len}", request.TelegramId,
                bytes.Length);
            return new UpsertTelegramBotUserProfilePhotoResponse { Updated = false };
        }

        var tgUser = await telegramBotUserQueryService.GetByTelegramId(request.TelegramId, cancellationToken);
        if (tgUser is null)
        {
            logger.LogWarning("Upsert profile photo: TelegramBotUser not found for {TelegramId}", request.TelegramId);
            return new UpsertTelegramBotUserProfilePhotoResponse { Updated = false };
        }

        var mime = string.IsNullOrWhiteSpace(request.ProfilePhotoMimeType)
            ? "image/jpeg"
            : request.ProfilePhotoMimeType.Trim();
        if (mime.Length > 64)
            mime = mime[..64];

        var unique = string.IsNullOrWhiteSpace(request.ProfilePhotoFileUniqueId)
            ? null
            : request.ProfilePhotoFileUniqueId.Trim();
        if (unique is { Length: > 128 })
            unique = unique[..128];

        var existingMeta =
            await profilePhotoQuery.GetIdAndFileUniqueIdNoTrackingAsync(tgUser.Id, cancellationToken);

        if (existingMeta is not null
            && !string.IsNullOrEmpty(unique)
            && !string.IsNullOrEmpty(existingMeta.TelegramFileUniqueId)
            && string.Equals(existingMeta.TelegramFileUniqueId, unique, StringComparison.Ordinal))
        {
            return new UpsertTelegramBotUserProfilePhotoResponse { Updated = false };
        }

        if (existingMeta is null)
        {
            var row = new TelegramBotUserProfilePhoto
            {
                TelegramBotUserId = tgUser.Id,
                ImageBytes = bytes,
                MimeType = mime,
                TelegramFileUniqueId = unique
            };
            await photoCommand.Add(row, saveChanges: true, cancellationToken);
            logger.LogInformation("Stored Telegram profile photo for TelegramId {TelegramId}", request.TelegramId);
            return new UpsertTelegramBotUserProfilePhotoResponse { Updated = true };
        }

        var tracked = await profilePhotoQuery.GetByTelegramBotUserIdForUpdateAsync(tgUser.Id, cancellationToken);
        if (tracked is null)
            return new UpsertTelegramBotUserProfilePhotoResponse { Updated = false };

        tracked.ImageBytes = bytes;
        tracked.MimeType = mime;
        tracked.TelegramFileUniqueId = unique;
        await photoCommand.Update(tracked, saveChanges: true, cancellationToken);
        logger.LogInformation("Updated Telegram profile photo for TelegramId {TelegramId}", request.TelegramId);
        return new UpsertTelegramBotUserProfilePhotoResponse { Updated = true };
    }

    public async Task<TelegramBotUserProfilePhotoMetaResponse?> GetMetaByTelegramIdAsync(long telegramId,
        CancellationToken cancellationToken)
    {
        var tgUser = await telegramBotUserQueryService.GetByTelegramId(telegramId, cancellationToken);
        if (tgUser is null)
            return null;

        var summary = await profilePhotoQuery.GetSummaryNoTrackingAsync(tgUser.Id, cancellationToken);
        return new TelegramBotUserProfilePhotoMetaResponse
        {
            TelegramId = telegramId,
            HasPhoto = summary is not null,
            TelegramFileUniqueId = summary?.TelegramFileUniqueId,
            MimeType = summary?.MimeType,
            LastUpdate = summary?.LastUpdate
        };
    }

    public async Task<(byte[] Bytes, string MimeType)?> GetImageByTelegramIdAsync(long telegramId,
        CancellationToken cancellationToken)
    {
        var tgUser = await telegramBotUserQueryService.GetByTelegramId(telegramId, cancellationToken);
        if (tgUser is null)
            return null;

        var row = await profilePhotoQuery.GetByTelegramBotUserIdNoTrackingAsync(tgUser.Id, cancellationToken);
        if (row is null || row.ImageBytes.Length == 0)
            return null;

        return (row.ImageBytes, row.MimeType);
    }

    public async Task<TelegramBotUserProfilePhotoIndexResponse> GetPhotoIndexAsync(CancellationToken cancellationToken)
    {
        var ids = await profilePhotoQuery.GetTelegramIdsWithProfilePhotoAsync(cancellationToken);
        return new TelegramBotUserProfilePhotoIndexResponse
        {
            TelegramIdsWithPhoto = ids.OrderBy(x => x).ToList()
        };
    }

    public async Task ApplyHasProfilePhotoFlagsAsync(IReadOnlyList<TelegramBotUserDto> users,
        CancellationToken cancellationToken)
    {
        if (users.Count == 0)
            return;

        var withPhoto = await profilePhotoQuery.GetTelegramIdsWithProfilePhotoAsync(cancellationToken);
        foreach (var user in users)
            user.HasProfilePhoto = user.TelegramId > 0 && withPhoto.Contains(user.TelegramId);
    }
}
