using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;

namespace DataGateMonitor.Services.Users.Interfaces;

public interface IFreeTierEnforcementOverviewService
{
    /// <summary>
    /// Every Free/Default user who is not compliant (not merged, not channel-subscribed) — i.e. every
    /// user the enforcement job would kill on its next run, whether or not they are connected right now.
    /// </summary>
    Task<GetFreeTierEnforcementCandidatesResponse> GetCandidatesAsync(CancellationToken ct = default);

    Task<GetFreeTierDisconnectLogResponse> GetDisconnectLogAsync(
        GetFreeTierDisconnectLogRequest request, CancellationToken ct = default);
}
