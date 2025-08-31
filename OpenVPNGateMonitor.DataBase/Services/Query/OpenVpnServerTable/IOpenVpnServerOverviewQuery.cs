using OpenVPNGateMonitor.Models.Helpers.Services;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerOverviewQuery
{
    Task<List<OpenVpnServerWithStatus>> GetAllOpenVpnServersWithStatusAsync(CancellationToken ct);
    Task<OpenVpnServerWithStatus> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct);
    Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct);
}
