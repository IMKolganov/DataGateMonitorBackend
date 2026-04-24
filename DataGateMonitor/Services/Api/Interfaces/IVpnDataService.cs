using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Interfaces;

public interface IVpnDataService
{
    Task<VpnServer> AddVpnServer(VpnServer openVpnServer, List<int> quotaPlanIds, List<int> tagIds, CancellationToken ct);
    Task<VpnServer> UpdateVpnServer(VpnServer openVpnServer, List<int> quotaPlanIds, List<int> tagIds, CancellationToken ct);
    Task<bool> DeleteVpnServer(int vpnServerId, CancellationToken ct);
}