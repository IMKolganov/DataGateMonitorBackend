using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TelegramBotUserController(ITelegramUserService telegramUserService) : ControllerBase
{
    [HttpPost("RegisterUser")]
    public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> RegisterUser([FromBody] RegisterUserRequest request, 
        CancellationToken cancellationToken)
    {
        var telegramBotUser = await telegramUserService.RegisterUserAsync(request.Adapt<TelegramBotUser>(), 
            cancellationToken);
        
        return Ok(ApiResponse<RegisterUserResponse>.SuccessResponse(telegramBotUser.Adapt<RegisterUserResponse>()));
    }

    [HttpGet("GetAdmins")]
    public async Task<ActionResult<ApiResponse<GetAdminsResponse>>> GetAdmins(CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAdminsAsync(cancellationToken);
        return Ok(ApiResponse<GetAdminsResponse>.SuccessResponse(telegramBotUsers.Adapt<GetAdminsResponse>()));
    }
    
    [HttpGet("GetAllUsers")]
    public async Task<ActionResult<ApiResponse<GetAllUsersResponse>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAllUsersAsync(cancellationToken);
        return Ok(ApiResponse<GetAllUsersResponse>.SuccessResponse(telegramBotUsers.Adapt<GetAllUsersResponse>()));
    }
}