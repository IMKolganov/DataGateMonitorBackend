using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 Telegram bot users with Page/PageSize + PagedResponse.</summary>
[ApiController]
[Route("api/v2/tgbot-users")]
[Authorize(Roles = "Admin,App")]
[Authorize]
public class TelegramBotUsersV2Controller(
    ITelegramUserService telegramUserService,
    ITelegramBotUserProfilePhotoService telegramBotUserProfilePhotoService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<GetTelegramBotUsersV2Response>>> GetPage(
        [FromQuery] GetTelegramBotUsersV2Request request,
        CancellationToken cancellationToken)
    {
        var filter = new GetAllTelegramBotUsersRequest
        {
            Search = request.Search,
            TelegramId = request.TelegramId,
            Username = request.Username,
            IsAdmin = request.IsAdmin,
            IsBlocked = request.IsBlocked,
        };

        var users = await telegramUserService.GetAllUsersAsync(filter, cancellationToken);
        var all = users.Adapt<GetAllTelegramUsersResponse>().TelegramBotUsers;
        var page = PagedResponseFactory.FromItems(all, request.Page, request.PageSize);
        await telegramBotUserProfilePhotoService.ApplyHasProfilePhotoFlagsAsync(page.Items, cancellationToken);

        return Ok(ApiResponse<GetTelegramBotUsersV2Response>.SuccessResponse(new GetTelegramBotUsersV2Response
        {
            TelegramBotUsers = page
        }));
    }
}
