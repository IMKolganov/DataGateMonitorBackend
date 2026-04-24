using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.ClientApplicationTable;

public class ClientApplicationQueryService(IQueryService<ClientApplication, int> q) : IClientApplicationQueryService
{
    public Task<List<ClientApplication>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);
    
    public Task<List<ClientApplication>> GetAllIsNotRevoked(CancellationToken ct)
        => q.Query()
            .Where(x => x.IsRevoked == false)
            .ToListAsync(ct);

    public Task<ClientApplication?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<ClientApplication?> GetByName(string name, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.Name == name, ct);

    public Task<ClientApplication?> GetByClientId(string clientId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.ClientId == clientId, ct);

    public Task<ClientApplication?> GetBySystemByClientId(string clientId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x =>
                x.ClientId == clientId && x.IsSystem && x.IsRevoked == false, ct);
    public Task<ClientApplication?> IsSystemConfigured(CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.IsSystem, ct);
    public Task<IPagedResult<ClientApplication>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}