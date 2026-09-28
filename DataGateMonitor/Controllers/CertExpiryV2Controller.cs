using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 cert-expiry endpoints with Page/PageSize + PagedResponse canon.</summary>
[ApiController]
[Route("api/v2/cert-expiry")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class CertExpiryV2Controller(
    ICertExpiryRunHistoryStore runHistoryStore) : BaseController
{
    [HttpGet("runs")]
    public ActionResult<ApiResponse<GetCertExpiryRunsV2Response>> GetRuns(
        [FromQuery] GetCertExpiryRunsV2Request request)
    {
        var response = new GetCertExpiryRunsV2Response
        {
            Runs = runHistoryStore.ListPage(request.Page, request.PageSize, request.VpnServerId)
        };

        return Ok(ApiResponse<GetCertExpiryRunsV2Response>.SuccessResponse(response));
    }
}
