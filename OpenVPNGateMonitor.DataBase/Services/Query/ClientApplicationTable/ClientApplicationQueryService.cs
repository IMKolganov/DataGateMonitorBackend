using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;

public class ClientApplicationQueryService(IQueryService<ClientApplication, int> q) : IClientApplicationQueryService
{
    public Task<List<ClientApplication>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);
    
    public Task<List<ClientApplication>> GetAllIsNotRevoked(CancellationToken ct)
        => q.Where(x => x.IsRevoked == false, ct: ct);

    public Task<ClientApplication?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<ClientApplication?> GetByName(string name, CancellationToken ct)
        => q.FirstOrDefault(x => x.Name == name, ct: ct);

    public Task<ClientApplication?> GetByClientId(string clientId, CancellationToken ct)
        => q.FirstOrDefault(x => x.ClientId == clientId, ct: ct);

    public Task<ClientApplication?> GetBySystemByClientId(string clientId, CancellationToken ct)
        => q.FirstOrDefault(x => 
            x.ClientId == clientId && x.IsSystem && x.IsRevoked == false, ct: ct);
    public Task<ClientApplication?> IsSystemConfigured(CancellationToken ct)
        => q.FirstOrDefault(x => x.IsSystem, ct: ct);
    public Task<IPagedResult<ClientApplication>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}