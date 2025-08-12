using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;

public class ClientApplicationQueryService(IQueryService<ClientApplication, int> q) : IClientApplicationQueryService
{
    public Task<List<ClientApplication>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);
    
    public Task<List<ClientApplication>> GetAllIsNotRevokedAsync(CancellationToken ct)
        => q.Query().Where(x => x.IsRevoked == false).ToListAsync(ct);

    public Task<ClientApplication?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<ClientApplication?> GetByNameAsync(string name, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.Name == name, ct);

    public Task<ClientApplication?> GetByClientIdAsync(string clientId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.ClientId == clientId, ct);

    public Task<ClientApplication?> GetBySystemByClientIdAsync(string clientId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => 
            x.ClientId == clientId && x.IsSystem && x.IsRevoked == false, ct);
    public Task<ClientApplication?> IsSystemConfiguredAsync(CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.IsSystem, ct);
    public Task<PagedResult<ClientApplication>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}