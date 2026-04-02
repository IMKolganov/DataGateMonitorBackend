using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerOverviewQuery
{
    /// <param name="requireQuotaPlanAssignment">When true, only servers present in <c>QuotaPlanAllowedServers</c> are returned.</param>
    Task<List<OpenVpnServerWithStatusDto>> GetAllOpenVpnServersWithStatusAsync(bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false, CancellationToken ct = default);
    Task<OpenVpnServerWithStatusDto> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct = default);
    Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct = default);
}