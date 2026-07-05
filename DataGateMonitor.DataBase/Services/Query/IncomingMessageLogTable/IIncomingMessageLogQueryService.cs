using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;

public interface IIncomingMessageLogQueryService
{
    Task<List<IncomingMessageLog>> GetAll(CancellationToken ct);
    Task<IncomingMessageLog?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<IncomingMessageLog>> GetPageByTelegramId(long telegramId, int page, int pageSize,
        CancellationToken ct);
    Task<IPagedResult<IncomingMessageLog>> GetPage(int page, int pageSize, CancellationToken ct);
    Task<IPagedResult<IncomingMessageLog>> GetPage(GetAllMessagesRequest request, CancellationToken ct);
}