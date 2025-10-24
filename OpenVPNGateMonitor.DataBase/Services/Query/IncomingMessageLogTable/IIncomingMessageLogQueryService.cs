using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;

public interface IIncomingMessageLogQueryService
{
    Task<List<IncomingMessageLog>> GetAllAsync(CancellationToken ct);
    Task<IncomingMessageLog?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<IncomingMessageLog>> GetByTelegramIdAsync(long telegramId, CancellationToken ct);
    Task<IPagedResult<IncomingMessageLog>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}