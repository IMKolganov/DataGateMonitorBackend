using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

/// <summary>v2 admin TV login sessions with PagedResponse canon.</summary>
public sealed class GetAdminTvLoginSessionsV2Response
{
    public PagedResponse<AdminTvLoginSessionDto> Sessions { get; set; } = new();
}
