using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;

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
        => q.Page(
            page,
            pageSize,
            null,
            o => o.OrderByDescending(x => x.Id),
            true,
            ct
        );
}