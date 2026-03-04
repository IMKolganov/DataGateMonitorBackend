using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerOverviewQuery
{
    Task<List<OpenVpnServerWithStatusDto>> GetAllOpenVpnServersWithStatusAsync(bool includeDeleted = false, CancellationToken ct = default);
    Task<OpenVpnServerWithStatusDto> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct = default);
    Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct = default);
}