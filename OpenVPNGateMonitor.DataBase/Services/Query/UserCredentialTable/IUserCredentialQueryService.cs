using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;

public interface IUserCredentialQueryService
{
    Task<List<UserCredential>> GetAllAsync(CancellationToken ct);
    Task<UserCredential?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserCredential?> GetByNormalizedLogin(string normalizedLogin, CancellationToken ct);
    Task<UserCredential?> GetByUserId(int userId, CancellationToken ct);
    Task<bool> AnyByUserId(int userId, CancellationToken ct);

    Task<IPagedResult<UserCredential>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}