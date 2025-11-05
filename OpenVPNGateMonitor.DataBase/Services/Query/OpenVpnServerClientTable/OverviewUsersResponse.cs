using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public class OverviewUsersResponse//todo: move to shared models
{
    public List<OverviewUserDto> OverviewUserItems { get; set; } = new();
}