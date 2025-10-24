using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserTable;

public interface IUserQueryService
{
    Task<List<User>> GetAllAsync(CancellationToken ct);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);

    Task<IPagedResult<User>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}