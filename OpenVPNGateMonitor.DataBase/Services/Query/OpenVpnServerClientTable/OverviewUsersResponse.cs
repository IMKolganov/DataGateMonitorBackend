using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public class OverviewUsersResponse
{
    private List<OverviewUserItem> OverviewUserItems { get; set; } = new();
}