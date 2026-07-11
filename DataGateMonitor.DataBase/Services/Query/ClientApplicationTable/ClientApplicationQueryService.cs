using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.ClientApplicationTable;

public class ClientApplicationQueryService(IQueryService<ClientApplication, int> q) : IClientApplicationQueryService
{
    public Task<List<ClientApplication>> GetAll(CancellationToken ct)
        => GetFiltered(new GetAllApplicationsRequest(), ct);

    public async Task<List<ClientApplication>> GetFiltered(GetAllApplicationsRequest request, CancellationToken ct)
    {
        var query = q.Query();

        var namePattern = GridFilterHelper.ContainsPattern(request.Name);
        if (namePattern != null)
            query = query.Where(x => EF.Functions.ILike(x.Name, namePattern));

        var clientIdPattern = GridFilterHelper.ContainsPattern(request.ClientId);
        if (clientIdPattern != null)
            query = query.Where(x => EF.Functions.ILike(x.ClientId, clientIdPattern));

        if (request.IsRevoked.HasValue)
            query = query.Where(x => x.IsRevoked == request.IsRevoked.Value);

        return await query
            .OrderBy(x => x.Id)
            .AsNoTracking()
            .ToListAsync(ct);
    }
    
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