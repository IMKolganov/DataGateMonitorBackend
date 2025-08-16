using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;

public interface IClientApplicationQueryService
{
    Task<List<ClientApplication>> GetAllAsync(CancellationToken ct);
    Task<List<ClientApplication>> GetAllIsNotRevokedAsync(CancellationToken ct);
    Task<ClientApplication?> GetByIdAsync(int id, CancellationToken ct);
    Task<ClientApplication?> GetByNameAsync(string name, CancellationToken ct);
    Task<ClientApplication?> GetByClientIdAsync(string clientId, CancellationToken ct);
    Task<ClientApplication?> GetBySystemByClientIdAsync(string clientId, CancellationToken ct);
    Task<ClientApplication?> IsSystemConfiguredAsync(CancellationToken ct);
    Task<IPagedResult<ClientApplication>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}