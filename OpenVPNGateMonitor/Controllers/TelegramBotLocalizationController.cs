using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TelegramBotLocalizationController(ILocalizationService localization) : ControllerBase
{
    [HttpPost("SetTelegramUserLanguage")]
    public async Task<IActionResult> SetTelegramUserLanguageAsync([FromBody] SetTelegramUserLanguageRequest request, 
        CancellationToken cancellationToken = default)
    {
        var telegramUserLanguagePreference = await localization.SetTelegramUserLanguageAsync(
            request.Adapt<TelegramUserLanguagePreference>(), cancellationToken);
        
        return Ok(ApiResponse<SetTelegramUserLanguageResponse>.SuccessResponse(
            telegramUserLanguagePreference.Adapt<SetTelegramUserLanguageResponse>()));
    }

    [HttpGet("GetTelegramUserLanguage/{telegramId:int}")]
    public async Task<IActionResult> GetTelegramUserLanguageAsync([FromRoute] GetTelegramUserLanguageRequest request,
        CancellationToken cancellationToken = default)
    {
        var telegramBotUsers = await localization.GetTelegramUserLanguageAsync(
            request.TelegramId, cancellationToken);
        return Ok(ApiResponse<GetTelegramUserLanguageResponse>.SuccessResponse(
            telegramBotUsers.Adapt<GetTelegramUserLanguageResponse>()));
    }
    
    [HttpGet("IsExistTelegramUserLanguagePreference/{telegramId:int}")]
    public async Task<IActionResult> IsExistTelegramUserLanguagePreferenceAsync(
        [FromRoute] IsExistTelegramUserLanguagePreferenceRequest request, CancellationToken cancellationToken = default)
    {
        var telegramBotUsers = await localization.IsExistTelegramUserLanguagePreferenceAsync(
            request.TelegramId, cancellationToken);
        return Ok(ApiResponse<IsExistTelegramUserLanguagePreferenceResponse>.SuccessResponse(
            telegramBotUsers.Adapt<IsExistTelegramUserLanguagePreferenceResponse>()));
    }
    
    [HttpGet("GetTextForTelegramUser/{telegramId:int}/{key:int}")]
    public async Task<IActionResult> GetTextAsync([FromRoute] GetTextForTelegramUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var telegramBotUsers = await localization.GetTextForTelegramUser(request.Key,
            request.TelegramId, cancellationToken);
        return Ok(ApiResponse<GetTextForTelegramUserResponse>.SuccessResponse(
            telegramBotUsers.Adapt<GetTextForTelegramUserResponse>()));
    }
}