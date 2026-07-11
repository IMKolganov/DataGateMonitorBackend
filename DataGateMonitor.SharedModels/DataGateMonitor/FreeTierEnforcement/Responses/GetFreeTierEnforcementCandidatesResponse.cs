using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;

public sealed class GetFreeTierEnforcementCandidatesResponse
{
    public List<FreeTierEnforcementCandidateDto> Candidates { get; set; } = [];
    public int TotalCount { get; set; }
    public int ConnectedCount { get; set; }
}
