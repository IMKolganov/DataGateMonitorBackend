using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Requests;

/// <summary>v2 paged free-tier enforcement candidates.</summary>
public class GetFreeTierCandidatesV2Request
{
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "pageSize must be between 1 and 500.")]
    public int PageSize { get; set; } = 20;
}
