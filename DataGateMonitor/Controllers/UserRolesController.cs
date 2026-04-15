using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Models;
using DataGateMonitor.Services.UserRoles;
using DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/user-roles")]
[Authorize(Roles = "Admin,App")]
public class UserRolesController(IUserRoleManagementService userRoleManagementService) : BaseController
{
    /// <summary>List all roles defined in the system.</summary>
    [HttpGet("get-all-roles")]
    public async Task<ActionResult<ApiResponse<RolesResponse>>> GetAllRoles(CancellationToken ct)
    {
        var entities = await userRoleManagementService.GetAllRolesAsync(ct);
        var response = new RolesResponse { Roles = entities.Adapt<List<RoleDto>>() };
        return Ok(ApiResponse<RolesResponse>.SuccessResponse(response));
    }

    /// <summary>Get the current role assignment for a user.</summary>
    [HttpGet("by-user/{userId:int}")]
    public async Task<ActionResult<ApiResponse<UserRoleAssignmentResponse>>> GetByUserId(int userId,
        CancellationToken ct)
    {
        try
        {
            var pair = await userRoleManagementService.GetAssignmentByUserIdAsync(userId, ct);
            if (pair is null)
                return Ok(ApiResponse<UserRoleAssignmentResponse>.SuccessResponse(new UserRoleAssignmentResponse()));

            var (ur, role) = pair.Value;
            var dto = ToAssignmentDto(ur, role);
            return Ok(ApiResponse<UserRoleAssignmentResponse>.SuccessResponse(
                new UserRoleAssignmentResponse { Assignment = dto }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<UserRoleAssignmentResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Replace the user's roles with a single role.</summary>
    [HttpPut("set")]
    public async Task<ActionResult<ApiResponse<UserRoleAssignmentResponse>>> SetUserRole(
        [FromBody] SetUserRoleRequest request,
        CancellationToken ct)
    {
        try
        {
            var (ur, role) = await userRoleManagementService.SetUserRoleAsync(request.UserId, request.RoleId, ct);
            var dto = ToAssignmentDto(ur, role);
            return Ok(ApiResponse<UserRoleAssignmentResponse>.SuccessResponse(
                new UserRoleAssignmentResponse { Assignment = dto }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    private static UserRoleAssignmentDto ToAssignmentDto(UserRole ur, Role role) =>
        new()
        {
            UserId = ur.UserId,
            RoleId = ur.RoleId,
            RoleName = role.Name
        };
}
