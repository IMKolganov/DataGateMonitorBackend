using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;

public class IncomingMessageLogQueryService(IQueryService<IncomingMessageLog, int> q) : IIncomingMessageLogQueryService
{
    public Task<List<IncomingMessageLog>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<IncomingMessageLog?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<IPagedResult<IncomingMessageLog>> GetPageByTelegramIdAsync(long telegramId, int page, int pageSize, 
        CancellationToken ct)
    {
        return q.PageAsync(
            page: page,
            pageSize: pageSize,
            predicate: x => x.TelegramId == telegramId,
            ct: ct);
    }

    public Task<IPagedResult<IncomingMessageLog>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}