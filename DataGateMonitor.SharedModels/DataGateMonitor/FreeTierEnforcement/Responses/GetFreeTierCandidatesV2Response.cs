using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;

/// <summary>v2 paged free-tier candidates with PagedResponse canon.</summary>
public sealed class GetFreeTierCandidatesV2Response
{
    public PagedResponse<FreeTierEnforcementCandidateDto> Candidates { get; set; } = new();

    /// <summary>Count of candidates currently connected (across the full set, not just the page).</summary>
    public int ConnectedCount { get; set; }
}
