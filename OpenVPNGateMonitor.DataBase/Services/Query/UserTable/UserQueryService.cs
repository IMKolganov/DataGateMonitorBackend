using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserTable;

public class UserQueryService(IQueryService<User, int> q) : IUserQueryService
{
    public Task<List<User>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<IPagedResult<User>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}