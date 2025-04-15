using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.TelegramBotUser.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TelegramBotUserController(ITelegramUserService telegramUserService) : ControllerBase
{
    [HttpPost("RegisterUser")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request, 
        CancellationToken cancellationToken = default)
    {
        var telegramBotUser = await telegramUserService.RegisterUserAsync(request.Adapt<TelegramBotUser>(), 
            cancellationToken);
        
        return Ok(ApiResponse<RegisterUserResponse>.SuccessResponse(telegramBotUser.Adapt<RegisterUserResponse>()));
    }

    [HttpGet("GetAdmins")]
    public async Task<IActionResult> Set(CancellationToken cancellationToken = default)
    {
        var telegramBotUsers = await telegramUserService.GetAdminsAsync(cancellationToken);
        return Ok(ApiResponse<GetAdminsResponse>.SuccessResponse(telegramBotUsers.Adapt<GetAdminsResponse>()));
    }
}