using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public class OverviewUsersResponse
{
    public List<OverviewUserItem> OverviewUserItems { get; set; } = new();
}