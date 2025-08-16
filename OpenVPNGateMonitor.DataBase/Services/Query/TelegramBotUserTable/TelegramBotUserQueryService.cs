using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;

public class TelegramBotUserQueryService(IQueryService<TelegramBotUser, int> q) : ITelegramBotUserQueryService
{
    public Task<List<TelegramBotUser>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);
    
    public Task<List<TelegramBotUser>> GetAllAdminsAsync(CancellationToken ct)
        => q.Query().Where(x => x.IsAdmin == true).ToListAsync(ct);

    public Task<TelegramBotUser?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<bool> AnyByTelegramIdAsync(long telegramId, CancellationToken ct)
        => q.AnyAsync(x=> x.TelegramId == telegramId, ct: ct);

    public Task<TelegramBotUser?> GetByTelegramIdAsync(long telegramId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.TelegramId == telegramId, ct);
    
    public Task<IPagedResult<TelegramBotUser>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}