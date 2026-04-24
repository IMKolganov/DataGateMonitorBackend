using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnSummaryStatService
{
    Task<OpenVpnSummaryStats> GetSummaryStatsAsync(VpnServer openVpnServer,
        CancellationToken cancellationToken);
}