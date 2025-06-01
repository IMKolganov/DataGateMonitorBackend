using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnSummaryStatService
{
    Task<OpenVpnSummaryStats> GetSummaryStatsAsync(OpenVpnServer openVpnServer,
        CancellationToken cancellationToken);
}