using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;

public interface IUserIdentityLinkQueryService
{
    Task<List<UserIdentityLink>> GetAll(CancellationToken ct);
    Task<UserIdentityLink?> GetById(int id, CancellationToken ct);
    Task<UserIdentityLink?> GetByProviderAndExternalId(string provider, string externalId, CancellationToken ct);
    public Task<UserIdentityLink?> GetByExternalId(string externalId, CancellationToken ct);
    Task<UserIdentityLink?> GetByUserId(int userId, CancellationToken ct);
    Task<bool> AnyByUserId(int userId, CancellationToken ct);

    Task<IPagedResult<UserIdentityLink>> GetPage(int page, int pageSize, CancellationToken ct);
}