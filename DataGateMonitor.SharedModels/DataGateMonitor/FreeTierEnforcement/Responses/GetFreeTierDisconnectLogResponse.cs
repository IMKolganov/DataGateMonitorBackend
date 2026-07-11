using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;

public sealed class GetFreeTierDisconnectLogResponse
{
    public PagedResponse<FreeTierDisconnectLogEntryDto> Entries { get; set; } = new();
}
