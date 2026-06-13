using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public sealed class OverviewUsersResponse
{
    public List<OverviewUserDto> OverviewUserItems { get; set; } = new();
}
