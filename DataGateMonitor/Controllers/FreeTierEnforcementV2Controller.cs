using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 free-tier candidates with Page/PageSize + PagedResponse.</summary>
[ApiController]
[Route("api/v2/free-tier-enforcement")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class FreeTierEnforcementV2Controller(IFreeTierEnforcementOverviewService overviewService) : BaseController
{
    [HttpGet("candidates")]
    public async Task<ActionResult<ApiResponse<GetFreeTierCandidatesV2Response>>> GetCandidates(
        [FromQuery] GetFreeTierCandidatesV2Request request,
        CancellationToken ct)
    {
        var full = await overviewService.GetCandidatesAsync(ct);
        var response = new GetFreeTierCandidatesV2Response
        {
            Candidates = PagedResponseFactory.FromItems(full.Candidates, request.Page, request.PageSize),
            ConnectedCount = full.ConnectedCount
        };

        return Ok(ApiResponse<GetFreeTierCandidatesV2Response>.SuccessResponse(response));
    }
}
