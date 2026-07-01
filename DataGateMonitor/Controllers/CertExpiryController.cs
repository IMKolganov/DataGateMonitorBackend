using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/cert-expiry")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class CertExpiryController(
    ICertExpiryScheduledCheckRunner checkRunner,
    ICertExpiryRunHistoryStore runHistoryStore,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpPost("check")]
    public async Task<ActionResult<ApiResponse<CertExpiryCheckRunResponse>>> RunCheck(
        [FromBody] RunCertExpiryCheckRequest request,
        CancellationToken ct)
    {
        if (request.VpnServerId is int serverId
            && await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<CertExpiryCheckRunResponse>(
                User, vpnServerAccessQueryService, serverId, ct) is { } deny)
        {
            return deny;
        }

        var result = await checkRunner.RunCheckAsync(request, ct).ConfigureAwait(false);
        return Ok(ApiResponse<CertExpiryCheckRunResponse>.SuccessResponse(result));
    }

    [HttpGet("runs")]
    public ActionResult<ApiResponse<GetCertExpiryRunsResponse>> GetRuns(
        [FromQuery] int limit = 20,
        [FromQuery] int? vpnServerId = null)
    {
        var response = new GetCertExpiryRunsResponse
        {
            Runs = runHistoryStore.List(limit, vpnServerId).ToList()
        };

        return Ok(ApiResponse<GetCertExpiryRunsResponse>.SuccessResponse(response));
    }

    [HttpGet("runs/{runId:guid}")]
    public ActionResult<ApiResponse<CertExpiryCheckRunResponse>> GetRun([FromRoute] Guid runId)
    {
        var run = runHistoryStore.Get(runId);
        if (run is null)
            return NotFound(ApiResponse<CertExpiryCheckRunResponse>.ErrorResponse("Run not found."));

        return Ok(ApiResponse<CertExpiryCheckRunResponse>.SuccessResponse(run));
    }
}
