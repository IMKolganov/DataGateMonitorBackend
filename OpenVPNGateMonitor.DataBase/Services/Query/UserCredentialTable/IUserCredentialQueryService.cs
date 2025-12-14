using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;

public interface IUserCredentialQueryService
{
    Task<List<UserCredential>> GetAll(CancellationToken ct);
    Task<UserCredential?> GetById(int id, CancellationToken ct);
    Task<UserCredential?> GetByNormalizedLogin(string normalizedLogin, CancellationToken ct);
    Task<UserCredential?> GetByLogin(string login, CancellationToken ct);
    Task<UserCredential?> GetByUserId(int userId, CancellationToken ct);
    Task<bool> AnyByUserId(int userId, CancellationToken ct);
    Task<bool> LoginExists(string login, CancellationToken ct);

    Task<IPagedResult<UserCredential>> GetPage(int page, int pageSize, CancellationToken ct);
}