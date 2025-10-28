using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TelegramBotUserController(ITelegramUserService telegramUserService) : ControllerBase
{
    [HttpGet("UserExists/{telegramId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UserExists([FromRoute] TelegramUserActionRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await telegramUserService.GetUserByTelegramIdAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(user != null));
    }

    [HttpGet("GetUser/{telegramId}")]
    public async Task<ActionResult<ApiResponse<UserRequest>>> GetUser(
        [FromRoute] UserRequest request, CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetUserAsync(request.TelegramId,cancellationToken);
        return Ok(ApiResponse<UserResponse>.SuccessResponse(telegramBotUsers.Adapt<UserResponse>()));
    }

    
    [HttpGet("GetAdmins")]
    public async Task<ActionResult<ApiResponse<GetAdminsResponse>>> GetAdmins(CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAdminsAsync(cancellationToken);
        return Ok(ApiResponse<GetAdminsResponse>.SuccessResponse(telegramBotUsers.Adapt<GetAdminsResponse>()));
    }
    
    [HttpGet("GetAllUsers")]
    public async Task<ActionResult<ApiResponse<GetAllTelegramUsersResponse>>> GetAllUsers(
        CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAllUsersAsync(cancellationToken);
        return Ok(ApiResponse<GetAllTelegramUsersResponse>.SuccessResponse(
            telegramBotUsers.Adapt<GetAllTelegramUsersResponse>()));
    }
    
    [HttpPost("BlockUser")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockUser([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.BlockUserAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    [HttpPost("UnblockUser")]
    public async Task<ActionResult<ApiResponse<bool>>> UnblockUser([FromBody] TelegramUserActionRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.UnblockUserAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
    
    [HttpPost("SetAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> SetAdmin([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.SetAdminAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    [HttpPost("UnsetAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UnsetAdmin([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.UnsetAdminAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
}