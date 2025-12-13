using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth.Handlers;

public sealed class VpnServerAccessQueryService(
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService)
    : IVpnServerAccessQueryService
{
    public async Task<bool> UserHasAccessAsync(int userId, int vpnServerId, CancellationToken ct)
    {
        var userQuotaPlan = await userQuotaPlanQueryService.GetByUserId(userId, ct);
        if (userQuotaPlan is null)
            return false;

        var quotaPlanId = userQuotaPlan.QuotaPlanId;

        var allowed = await quotaPlanAllowedServerQueryService
            .GetByQuotaPlanIdAndServerId(quotaPlanId, vpnServerId, ct);

        return allowed is not null;
    }
}