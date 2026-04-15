using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class UserController(IUserService userService) : BaseController
{
    [HttpPost("register-from-tgbot")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> RegisterUser([FromBody] RegisterUserFromTgBotRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await userService.RegisterUserFromTgBot(request.Adapt<RegisterUserFromTgBotRequest>(), 
            cancellationToken);
        
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(response.Adapt<UsersResponse>()));
    }
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllUsersResponse>>> GetAllUsers(
        [FromQuery] GetAllUsersRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.GetUsersPage(request, cancellationToken);
        return Ok(ApiResponse<GetAllUsersResponse>.SuccessResponse(response));
    }
    
    [HttpGet("get-by-id/{id:int}")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> GetUserById([FromRoute]GetUserByIdRequest request, 
        CancellationToken cancellationToken)
    {
        var telegramBotUsers = await userService.GetUserById(request, cancellationToken);
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(telegramBotUsers.Adapt<UsersResponse>()));
    }
    
    [HttpGet("get-by-external-id/{externalId:int}")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> GetUserByExternalId(
        [FromRoute]GetUserByExternalIdRequest request, CancellationToken cancellationToken)
    {
        var telegramBotUsers = await userService.GetUserByExternalId(request, cancellationToken);
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(telegramBotUsers.Adapt<UsersResponse>()));
    }
}