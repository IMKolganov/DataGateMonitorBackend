using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;

public class VpnServerEventResponse
{
    public PagedResponse<VpnServerEventLogDto> Events { get; set; } = new();
}