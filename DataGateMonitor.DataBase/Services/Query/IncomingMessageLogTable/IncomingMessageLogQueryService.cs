using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;

public class IncomingMessageLogQueryService(IQueryService<IncomingMessageLog, int> q) : IIncomingMessageLogQueryService
{
    public Task<List<IncomingMessageLog>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<IncomingMessageLog?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<IPagedResult<IncomingMessageLog>> GetPageByTelegramId(long telegramId, int page, int pageSize, 
        CancellationToken ct)
    {
        return q.Page(
            page: page,
            pageSize: pageSize,
            predicate: x => x.TelegramId == telegramId,
            o => o.OrderByDescending(x => x.Id),
            true,
            ct: ct);
    }

    public Task<IPagedResult<IncomingMessageLog>> GetPage(int page, int pageSize, CancellationToken ct)
        => GetPage(new GetAllMessagesRequest { Page = page, PageSize = pageSize }, ct);

    public async Task<IPagedResult<IncomingMessageLog>> GetPage(
        GetAllMessagesRequest request,
        CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

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
                EF.Functions.ILike(x.MessageText, searchPattern) ||
                (x.Username != null && EF.Functions.ILike(x.Username, searchPattern)) ||
                (x.FirstName != null && EF.Functions.ILike(x.FirstName, searchPattern)) ||
                (x.LastName != null && EF.Functions.ILike(x.LastName, searchPattern)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResponse<IncomingMessageLog>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }
}