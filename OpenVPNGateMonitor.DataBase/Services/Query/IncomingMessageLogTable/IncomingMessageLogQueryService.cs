using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;

public class IncomingMessageLogQueryService(IQueryService<IncomingMessageLog, int> q) : IIncomingMessageLogQueryService
{
    public Task<List<IncomingMessageLog>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<IncomingMessageLog?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<List<IncomingMessageLog>> GetByTelegramIdAsync(long telegramId, CancellationToken ct)
        => q.Query()
            .Where(x => x.TelegramId == telegramId)
            .ToListAsync(ct);

    public Task<IPagedResult<IncomingMessageLog>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}