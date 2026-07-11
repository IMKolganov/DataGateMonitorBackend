using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.TelegramBotUserTable;

public class TelegramBotUserQueryService(IQueryService<TelegramBotUser, int> q) : ITelegramBotUserQueryService
{
    public Task<List<TelegramBotUser>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);
    
    public Task<List<TelegramBotUser>> GetAllAdmins(CancellationToken ct)
        => q.Query().Where(x => x.IsAdmin == true).ToListAsync(ct);

    public Task<TelegramBotUser?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<bool> AnyByTelegramId(long telegramId, CancellationToken ct)
        => q.Any(x=> x.TelegramId == telegramId, ct: ct);

    public Task<TelegramBotUser?> GetByTelegramId(long telegramId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.TelegramId == telegramId, ct);
    
    public Task<IPagedResult<TelegramBotUser>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);

    public async Task<List<TelegramBotUser>> GetFiltered(GetAllTelegramBotUsersRequest request, CancellationToken ct)
    {
        var query = q.Query();

        if (request.TelegramId.HasValue)
            query = query.Where(x => x.TelegramId == request.TelegramId.Value);

        var usernamePattern = GridFilterHelper.ContainsPattern(request.Username);
        if (usernamePattern != null)
            query = query.Where(x => x.Username != null && EF.Functions.ILike(x.Username, usernamePattern));

        var searchPattern = GridFilterHelper.ContainsPattern(request.Search);
        if (searchPattern != null)
        {
            query = query.Where(x =>
                (x.Username != null && EF.Functions.ILike(x.Username, searchPattern)) ||
                (x.FirstName != null && EF.Functions.ILike(x.FirstName, searchPattern)) ||
                (x.LastName != null && EF.Functions.ILike(x.LastName, searchPattern)));
        }

        if (request.IsAdmin.HasValue)
            query = query.Where(x => x.IsAdmin == request.IsAdmin.Value);

        if (request.IsBlocked.HasValue)
            query = query.Where(x => x.IsBlocked == request.IsBlocked.Value);

        return await query
            .OrderBy(x => x.Id)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}