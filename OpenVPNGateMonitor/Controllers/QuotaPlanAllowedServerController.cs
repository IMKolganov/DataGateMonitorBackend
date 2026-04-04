using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.QuotaPlans;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/quota-plan-allowed-servers")]
[Authorize]
public class QuotaPlanAllowedServerController(
    IQuotaPlanAllowedServerService service,
    IUserQuotaPlanQueryService userQuotaPlanQueryService) : BaseController
{
    /// <summary>Get paged list. Optional filter by quotaPlanId and/or vpnServerId (query).</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllQuotaPlanAllowedServersResponse>>> GetAll(
        [FromQuery] GetAllQuotaPlanAllowedServersRequest request,
        CancellationToken ct)
    {
        var response = await service.GetPageAsync(request, ct);
        return Ok(ApiResponse<GetAllQuotaPlanAllowedServersResponse>.SuccessResponse(response));
    }

    /// <summary>Get by id.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<ApiResponse<QuotaPlanAllowedServerResponse>>> GetById(int id, CancellationToken ct)
    {
        var response = await service.GetByIdAsync(id, ct);
        if (response is null)
            return NotFound(ApiResponse<QuotaPlanAllowedServerResponse>.ErrorResponse("Quota plan allowed server not found."));

        return Ok(ApiResponse<QuotaPlanAllowedServerResponse>.SuccessResponse(response));
    }

    /// <summary>Get all allowed servers for a quota plan. VpnUser may only query their own active plan.</summary>
    [Authorize(Roles = "Admin,App,VpnUser")]
    [HttpGet("get-by-quota-plan-id/{quotaPlanId:int}")]
    public async Task<ActionResult<ApiResponse<GetQuotaPlanAllowedServersByQuotaPlanIdResponse>>> GetByQuotaPlanId(
        int quotaPlanId,
        CancellationToken ct)
    {
        if (!HttpUserContext.IsPrivileged(User))
        {
            if (!HttpUserContext.TryGetUserId(User, out var userId))
                return Unauthorized(ApiResponse<GetQuotaPlanAllowedServersByQuotaPlanIdResponse>.ErrorResponse(
                    "User id missing from token."));
            var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
            if (uqp is null || uqp.QuotaPlanId != quotaPlanId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<GetQuotaPlanAllowedServersByQuotaPlanIdResponse>.ErrorResponse(
                        "You can only load allowed servers for your own quota plan."));
        }

        var items = await service.GetListByQuotaPlanIdAsync(quotaPlanId, ct);
        return Ok(ApiResponse<GetQuotaPlanAllowedServersByQuotaPlanIdResponse>.SuccessResponse(
            new GetQuotaPlanAllowedServersByQuotaPlanIdResponse { Items = items }));
    }

    /// <summary>Get all quota plans that allow a VPN server.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-by-vpn-server-id/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<GetQuotaPlanAllowedServersByVpnServerIdResponse>>> GetByVpnServerId(
        int vpnServerId,
        CancellationToken ct)
    {
        var items = await service.GetListByVpnServerIdAsync(vpnServerId, ct);
        return Ok(ApiResponse<GetQuotaPlanAllowedServersByVpnServerIdResponse>.SuccessResponse(
            new GetQuotaPlanAllowedServersByVpnServerIdResponse { Items = items }));
    }

    /// <summary>Create a new assignment (allow server for plan).</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<QuotaPlanAllowedServerResponse>>> Create(
        [FromBody] CreateOrUpdateQuotaPlanAllowedServerRequest request,
        CancellationToken ct)
    {
        var response = await service.CreateAsync(request, ct);
        return Ok(ApiResponse<QuotaPlanAllowedServerResponse>.SuccessResponse(response));
    }

    /// <summary>Update an existing assignment.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        [FromBody] CreateOrUpdateQuotaPlanAllowedServerRequest request,
        CancellationToken ct)
    {
        await service.UpdateAsync(request, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
    }

    /// <summary>Delete an assignment by id.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpDelete("delete/{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
    }
}
