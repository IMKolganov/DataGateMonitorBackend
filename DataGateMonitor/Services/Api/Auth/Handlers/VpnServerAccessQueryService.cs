using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;

namespace DataGateMonitor.Services.Api.Auth.Handlers;

public sealed class VpnServerAccessQueryService(
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService)
    : IVpnServerAccessQueryService
{
    public async Task<bool> UserHasAccessAsync(int userId, int vpnServerId, CancellationToken ct)
    {
        var userQuotaPlan = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
        if (userQuotaPlan is null)
            return false;

        var quotaPlanId = userQuotaPlan.QuotaPlanId;

        var allowed = await quotaPlanAllowedServerQueryService
            .GetByQuotaPlanIdAndServerId(quotaPlanId, vpnServerId, ct);

        return allowed is not null;
    }
}