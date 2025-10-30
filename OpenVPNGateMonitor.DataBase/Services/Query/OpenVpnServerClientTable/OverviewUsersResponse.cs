using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public class OverviewUsersResponse
{
    public List<OverviewUserItem> OverviewUserItems { get; set; } = new();
}