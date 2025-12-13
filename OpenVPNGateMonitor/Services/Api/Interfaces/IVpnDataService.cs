using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Interfaces;

public interface IVpnDataService
{
    Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer openVpnServer, List<int> quotaPlanIds, CancellationToken ct);
    Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer openVpnServer, List<int> quotaPlanIds, CancellationToken ct);
    Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken ct);
}