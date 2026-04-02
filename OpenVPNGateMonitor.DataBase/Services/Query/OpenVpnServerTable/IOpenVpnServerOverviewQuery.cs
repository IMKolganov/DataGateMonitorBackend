using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerOverviewQuery
{
    /// <param name="requireQuotaPlanAssignment">When true, only servers present in <c>QuotaPlanAllowedServers</c> are returned. Ignored when <paramref name="restrictToQuotaPlanId"/> is set.</param>
    /// <param name="restrictToQuotaPlanId">When set, only servers linked to this quota plan are returned.</param>
    Task<List<OpenVpnServerWithStatusDto>> GetAllOpenVpnServersWithStatusAsync(bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false, int? restrictToQuotaPlanId = null, CancellationToken ct = default);
    Task<OpenVpnServerWithStatusDto> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct = default);
    Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct = default);
}