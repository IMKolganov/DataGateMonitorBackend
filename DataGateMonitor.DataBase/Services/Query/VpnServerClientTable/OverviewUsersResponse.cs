using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public class OverviewUsersResponse//todo: move to shared models
{
    public List<OverviewUserDto> OverviewUserItems { get; set; } = new();
}