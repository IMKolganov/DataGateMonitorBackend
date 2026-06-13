using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.TelegramBotUserProfilePhotoTable;

public sealed class TelegramBotUserProfilePhotoQueryService(IQueryService<TelegramBotUserProfilePhoto, int> q)
    : ITelegramBotUserProfilePhotoQueryService
{
    public Task<TelegramBotUserProfilePhoto?> GetByTelegramBotUserIdNoTrackingAsync(int telegramBotUserId,
        CancellationToken cancellationToken) =>
        q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TelegramBotUserId == telegramBotUserId, cancellationToken);

    public Task<TelegramBotUserProfilePhoto?> GetByTelegramBotUserIdForUpdateAsync(int telegramBotUserId,
        CancellationToken cancellationToken) =>
        q.Query()
            .FirstOrDefaultAsync(x => x.TelegramBotUserId == telegramBotUserId, cancellationToken);

    public async Task<TelegramBotUserProfilePhotoIdMeta?> GetIdAndFileUniqueIdNoTrackingAsync(int telegramBotUserId,
        CancellationToken cancellationToken)
    {
        return await q.Query()
            .AsNoTracking()
            .Where(x => x.TelegramBotUserId == telegramBotUserId)
            .Select(x => new TelegramBotUserProfilePhotoIdMeta(x.Id, x.TelegramFileUniqueId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TelegramBotUserProfilePhotoSummary?> GetSummaryNoTrackingAsync(int telegramBotUserId,
        CancellationToken cancellationToken)
    {
        return await q.Query()
            .AsNoTracking()
            .Where(x => x.TelegramBotUserId == telegramBotUserId)
            .Select(x => new TelegramBotUserProfilePhotoSummary(x.TelegramFileUniqueId, x.MimeType, x.LastUpdate))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<HashSet<long>> GetTelegramIdsWithProfilePhotoAsync(CancellationToken cancellationToken)
    {
        var ids = await q.Query()
            .AsNoTracking()
            .Where(x => x.TelegramBotUserId > 0)
            .Select(x => x.TelegramBotUser!.TelegramId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
