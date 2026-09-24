using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

/// <summary>v2 overview users with PagedResponse canon.</summary>
public sealed class OverviewUsersV2Response
{
    public PagedResponse<OverviewUserDto> Users { get; set; } = new();
}
