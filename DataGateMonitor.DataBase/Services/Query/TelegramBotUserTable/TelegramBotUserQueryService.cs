using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
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
}