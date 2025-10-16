using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController(INotificationService notificationService) : ControllerBase
{

    [HttpGet("NotifyAdmins")]
    public async Task<ActionResult<ApiResponse<int>>> NotifyAdminsAsync(NotificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await notificationService.NotifyAdminsAsync(request, ["web"], cancellationToken);
        // throw new NotImplementedException();
        // var telegramBotUsers = await notificationService.NotifyAdminsAsync(cancellationToken);
        return Ok(ApiResponse<int>.SuccessResponse(result));
    }
    
    [HttpGet("GetAllUsers")]
    public async Task<ActionResult<ApiResponse<GetAllUsersResponse>>> GetAllUsers(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // var telegramBotUsers = await notificationService.GetAllUsersAsync(cancellationToken);
        // return Ok(ApiResponse<GetAllUsersResponse>.SuccessResponse(telegramBotUsers.Adapt<GetAllUsersResponse>()));
    }
    
    [HttpPost("BlockUser")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockUser([FromBody] TelegramUserActionRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // var result = await notificationService.BlockUserAsync(request.TelegramId, cancellationToken);
        // return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
}