using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Users.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(IUserService userService) : BaseController
{
    
    [HttpPost("RegisterUserFromTgBot")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> RegisterUser([FromBody] RegisterUserFromTgBotRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await userService.RegisterUserFromTgBot(request.Adapt<RegisterUserFromTgBotRequest>(), 
            cancellationToken);
        
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(response.Adapt<UsersResponse>()));
    }
    [HttpGet("GetAllUsers")]
    public async Task<ActionResult<ApiResponse<GetAllUsersResponse>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var response = await userService.GetAllUsers(cancellationToken);
        return Ok(ApiResponse<GetAllUsersResponse>.SuccessResponse(response.Adapt<GetAllUsersResponse>()));
    }
    
    [HttpGet("GetUserById")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> GetUserById(GetUserByIdRequest request, 
        CancellationToken cancellationToken)
    {
        var telegramBotUsers = await userService.GetUserById(request, cancellationToken);
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(telegramBotUsers.Adapt<UsersResponse>()));
    }
    
    [HttpGet("GetUserByExternalId")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> GetUserByExternalId(GetUserByExternalIdRequest request,
        CancellationToken cancellationToken)
    {
        var telegramBotUsers = await userService.GetUserByExternalId(request, cancellationToken);
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(telegramBotUsers.Adapt<UsersResponse>()));
    }
    
}