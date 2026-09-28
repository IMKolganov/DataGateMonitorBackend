using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Auth.TvLogin;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 admin TV login sessions with Page/PageSize + PagedResponse.</summary>
[ApiController]
[Route("api/v2/admin/tv-login-sessions")]
[Authorize(Roles = "Admin")]
public sealed class TvLoginSessionsAdminV2Controller(ITvLoginAdminService adminService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetAdminTvLoginSessionsV2Response>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GetAdminTvLoginSessionsV2Response>>> List(
        [FromQuery] GetAdminTvLoginSessionsV2Request request,
        CancellationToken ct = default)
    {
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var result = await adminService.ListAsync(
            request.ApprovedUserId,
            request.Status,
            skip,
            request.PageSize,
            ct);

        var response = new GetAdminTvLoginSessionsV2Response
        {
            Sessions = PagedResponseFactory.Create(
                result.Sessions.ToList(),
                request.Page,
                request.PageSize,
                result.TotalCount)
        };

        return Ok(ApiResponse<GetAdminTvLoginSessionsV2Response>.SuccessResponse(response));
    }
}
