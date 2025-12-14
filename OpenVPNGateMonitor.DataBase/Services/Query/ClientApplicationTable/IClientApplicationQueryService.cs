using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;

public interface IClientApplicationQueryService
{
    Task<List<ClientApplication>> GetAll(CancellationToken ct);
    Task<List<ClientApplication>> GetAllIsNotRevoked(CancellationToken ct);
    Task<ClientApplication?> GetById(int id, CancellationToken ct);
    Task<ClientApplication?> GetByName(string name, CancellationToken ct);
    Task<ClientApplication?> GetByClientId(string clientId, CancellationToken ct);
    Task<ClientApplication?> GetBySystemByClientId(string clientId, CancellationToken ct);
    Task<ClientApplication?> IsSystemConfigured(CancellationToken ct);
    Task<IPagedResult<ClientApplication>> GetPage(int page, int pageSize, CancellationToken ct);
}