using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerOverviewQuery
{
    Task<List<OpenVpnServerWithStatusDto>> GetAllOpenVpnServersWithStatusAsync(CancellationToken ct);
    Task<OpenVpnServerWithStatusDto> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct);
    Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct);
}