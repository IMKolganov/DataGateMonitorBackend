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

    [HttpGet("GetTelegramUserLanguage/{telegramId}")]
    public async Task<IActionResult> GetTelegramUserLanguageAsync(
        [FromRoute] GetTelegramUserLanguageRequest request,
        CancellationToken cancellationToken = default)
    {
        var language = await localization.GetTelegramUserLanguageAsync(request.TelegramId, cancellationToken);

        var response = new GetTelegramUserLanguageResponse
        {
            PreferredLanguage = (SharedModels.TelegramBotLocalization.Enums.Language)language
        };

        return Ok(ApiResponse<GetTelegramUserLanguageResponse>.SuccessResponse(response));
    }

    
    [HttpGet("IsExistTelegramUserLanguagePreference/{telegramId}")]
    public async Task<IActionResult> IsExistTelegramUserLanguagePreferenceAsync(
        [FromRoute] IsExistTelegramUserLanguagePreferenceRequest request, 
        CancellationToken cancellationToken = default)
    {
        var response = new IsExistTelegramUserLanguagePreferenceResponse
        {
            IsExistTelegramUserLanguagePreference = await localization.IsExistTelegramUserLanguagePreferenceAsync(
                request.TelegramId, cancellationToken)
        };

        return Ok(ApiResponse<IsExistTelegramUserLanguagePreferenceResponse>.SuccessResponse(response));
    }
    
    [HttpGet("GetTextForTelegramUser/{telegramId}/{key}")]
    public async Task<IActionResult> GetTextAsync([FromRoute] GetTextForTelegramUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new GetTextForTelegramUserResponse
        {
            Text = await localization.GetTextForTelegramUser(
                request.Key, request.TelegramId, cancellationToken)
        };

        return Ok(ApiResponse<GetTextForTelegramUserResponse>.SuccessResponse(response));
    }
}