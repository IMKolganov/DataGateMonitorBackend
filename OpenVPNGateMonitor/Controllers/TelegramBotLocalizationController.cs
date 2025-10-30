using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/tgbot-localizations")]
[Authorize]
public class TelegramBotLocalizationController(ILocalizationService localization) : ControllerBase
{
    [HttpPost("set-tg-user-language")]
    public async Task<ActionResult<ApiResponse<SetTelegramUserLanguageResponse>>> SetTelegramUserLanguageAsync(
        [FromBody] SetTelegramUserLanguageRequest request, 
        CancellationToken cancellationToken)
    {
        var telegramUserLanguagePreference = await localization.SetTelegramUserLanguageAsync(
            request.Adapt<TelegramUserLanguagePreference>(), cancellationToken);
        
        return Ok(ApiResponse<SetTelegramUserLanguageResponse>.SuccessResponse(
            telegramUserLanguagePreference.Adapt<SetTelegramUserLanguageResponse>()));
    }

    [HttpGet("get-tg-user-language/{telegramId}")]
    public async Task<ActionResult<ApiResponse<GetTelegramUserLanguageResponse>>> GetTelegramUserLanguageAsync(
        [FromRoute] GetTelegramUserLanguageRequest request,
        CancellationToken cancellationToken)
    {
        var language = await localization.GetTelegramUserLanguageAsync(request.TelegramId, cancellationToken);

        var response = new GetTelegramUserLanguageResponse
        {
            PreferredLanguage = language
        };

        return Ok(ApiResponse<GetTelegramUserLanguageResponse>.SuccessResponse(response));
    }

    
    [HttpGet("is-exist-tg-user-language-preference/{telegramId}")]
    public async Task<ActionResult<ApiResponse<IsExistTelegramUserLanguagePreferenceResponse>>> 
        IsExistTelegramUserLanguagePreferenceAsync(
        [FromRoute] IsExistTelegramUserLanguagePreferenceRequest request, 
        CancellationToken cancellationToken)
    {
        var response = new IsExistTelegramUserLanguagePreferenceResponse
        {
            IsExistTelegramUserLanguagePreference = await localization.IsExistTelegramUserLanguagePreferenceAsync(
                request.TelegramId, cancellationToken)
        };

        return Ok(ApiResponse<IsExistTelegramUserLanguagePreferenceResponse>.SuccessResponse(response));
    }
    
    [HttpGet("get-text-for-tg-user/{telegramId}/{key}")]
    public async Task<ActionResult<ApiResponse<GetTextForTelegramUserResponse>>> 
        GetTextAsync([FromRoute] GetTextForTelegramUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = new GetTextForTelegramUserResponse
        {
            Text = await localization.GetTextForTelegramUser(
                request.Key, request.TelegramId, cancellationToken)
        };

        return Ok(ApiResponse<GetTextForTelegramUserResponse>.SuccessResponse(response));
    }
}