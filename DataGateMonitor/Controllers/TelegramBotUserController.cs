using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/tgbot-users")]
[Authorize(Roles = "Admin,App")]
[Authorize]
public class TelegramBotUserController(ITelegramUserService telegramUserService) : BaseController
{
    [HttpGet("check-exists/{telegramId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UserExists([FromRoute] TelegramUserActionRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await telegramUserService.GetUserByTelegramIdAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(user != null));
    }

    [HttpGet("get/{telegramId}")]
    public async Task<ActionResult<ApiResponse<UserRequest>>> GetUser(
        [FromRoute] UserRequest request, CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetUserAsync(request.TelegramId,cancellationToken);
        return Ok(ApiResponse<UserResponse>.SuccessResponse(telegramBotUsers.Adapt<UserResponse>()));
    }

    
    [HttpGet("get-admins")]
    public async Task<ActionResult<ApiResponse<GetAdminsResponse>>> GetAdmins(CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAdminsAsync(cancellationToken);
        return Ok(ApiResponse<GetAdminsResponse>.SuccessResponse(telegramBotUsers.Adapt<GetAdminsResponse>()));
    }
    
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllTelegramUsersResponse>>> GetAllUsers(
        CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAllUsersAsync(cancellationToken);
        return Ok(ApiResponse<GetAllTelegramUsersResponse>.SuccessResponse(
            telegramBotUsers.Adapt<GetAllTelegramUsersResponse>()));
    }
    
    [HttpPost("block")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockUser([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.BlockUserAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    [HttpPost("unblock")]
    public async Task<ActionResult<ApiResponse<bool>>> UnblockUser([FromBody] TelegramUserActionRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.UnblockUserAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
    
    [HttpPost("set-admin")]
    public async Task<ActionResult<ApiResponse<bool>>> SetAdmin([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.SetAdminAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    [HttpPost("unset-admin")]
    public async Task<ActionResult<ApiResponse<bool>>> UnsetAdmin([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.UnsetAdminAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
}