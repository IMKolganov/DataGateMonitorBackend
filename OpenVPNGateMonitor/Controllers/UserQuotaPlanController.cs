using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/user-quota-plans")]
[Authorize]
public class UserQuotaPlanController(IUserQuotaPlanService userQuotaPlanService) : BaseController
{
    /// <summary>Get paged list of user-quota-plan assignments. Optional filter by userId (query).</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllUserQuotaPlansResponse>>> GetAll(
        [FromQuery] GetAllUserQuotaPlansRequest request,
        CancellationToken ct)
    {
        var response = await userQuotaPlanService.GetPageAsync(request, ct);
        return Ok(ApiResponse<GetAllUserQuotaPlansResponse>.SuccessResponse(response));
    }

    /// <summary>Get a user-quota-plan assignment by id.</summary>
    [Authorize(Roles = "Admin,App,VpnUser")]
    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<ApiResponse<UserQuotaPlanResponse>>> GetById(int id, CancellationToken ct)
    {
        var response = await userQuotaPlanService.GetByIdAsync(id, ct);
        if (response is null)
            return NotFound(ApiResponse<UserQuotaPlanResponse>.ErrorResponse("User quota plan not found."));

        if (!HttpUserContext.IsPrivileged(User))
        {
            if (!HttpUserContext.TryGetUserId(User, out var currentUserId))
                return Unauthorized(ApiResponse<UserQuotaPlanResponse>.ErrorResponse("User id missing from token."));
            if (response.UserQuotaPlan.UserId != currentUserId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<UserQuotaPlanResponse>.ErrorResponse("Access denied."));
        }

        return Ok(ApiResponse<UserQuotaPlanResponse>.SuccessResponse(response));
    }

    /// <summary>Get all quota-plan assignments for a user.</summary>
    [Authorize(Roles = "Admin,App,VpnUser")]
    [HttpGet("get-by-user-id/{userId:int}")]
    public async Task<ActionResult<ApiResponse<GetUserQuotaPlansByUserIdResponse>>> GetByUserId(
        int userId,
        CancellationToken ct)
    {
        if (!HttpUserContext.IsPrivileged(User))
        {
            if (!HttpUserContext.TryGetUserId(User, out var currentUserId))
                return Unauthorized(ApiResponse<GetUserQuotaPlansByUserIdResponse>.ErrorResponse(
                    "User id missing from token."));
            if (currentUserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<GetUserQuotaPlansByUserIdResponse>.ErrorResponse(
                        "You can only read your own quota plan assignments."));
        }

        var items = await userQuotaPlanService.GetListByUserIdAsync(userId, ct);
        return Ok(ApiResponse<GetUserQuotaPlansByUserIdResponse>.SuccessResponse(
            new GetUserQuotaPlansByUserIdResponse { Items = items }));
    }

    /// <summary>Create a new user-quota-plan assignment.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<UserQuotaPlanResponse>>> Create(
        [FromBody] CreateOrUpdateUserQuotaPlanRequest request,
        CancellationToken ct)
    {
        var response = await userQuotaPlanService.CreateAsync(request, ct);
        return Ok(ApiResponse<UserQuotaPlanResponse>.SuccessResponse(response));
    }

    /// <summary>Update an existing user-quota-plan assignment.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        [FromBody] CreateOrUpdateUserQuotaPlanRequest request,
        CancellationToken ct)
    {
        await userQuotaPlanService.UpdateAsync(request, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
    }

    /// <summary>Delete a user-quota-plan assignment by id.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpDelete("delete/{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(int id, CancellationToken ct)
    {
        await userQuotaPlanService.DeleteAsync(id, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
    }
}
