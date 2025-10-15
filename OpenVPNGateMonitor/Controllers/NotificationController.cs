using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController(INotificationService notificationService) : ControllerBase
{

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
    
    [HttpPost("BlockUser")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockUser([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramUserService.BlockUserAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
}