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
public class TelegramBotUserController(
    ITelegramUserService telegramUserService,
    ITelegramBotUserProfilePhotoService telegramBotUserProfilePhotoService) : BaseController
{
    [HttpGet("check-exists/{telegramId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UserExists([FromRoute] TelegramUserActionRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await telegramUserService.GetUserByTelegramIdAsync(request.TelegramId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(user != null));
    }

    [HttpGet("get/{telegramId}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUser(
        [FromRoute] UserRequest request, CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetUserAsync(request.TelegramId,cancellationToken);
        var response = telegramBotUsers.Adapt<UserResponse>();
        await telegramBotUserProfilePhotoService.ApplyHasProfilePhotoFlagsAsync(
            [response.TelegramBotUser], cancellationToken);
        return Ok(ApiResponse<UserResponse>.SuccessResponse(response));
    }

    
    [HttpGet("get-admins")]
    public async Task<ActionResult<ApiResponse<GetAdminsResponse>>> GetAdmins(CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAdminsAsync(cancellationToken);
        var response = telegramBotUsers.Adapt<GetAdminsResponse>();
        await telegramBotUserProfilePhotoService.ApplyHasProfilePhotoFlagsAsync(
            response.TelegramBotAdmins, cancellationToken);
        return Ok(ApiResponse<GetAdminsResponse>.SuccessResponse(response));
    }
    
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllTelegramUsersResponse>>> GetAllUsers(
        [FromQuery] GetAllTelegramBotUsersRequest request,
        CancellationToken cancellationToken)
    {
        var telegramBotUsers = await telegramUserService.GetAllUsersAsync(request, cancellationToken);
        var response = telegramBotUsers.Adapt<GetAllTelegramUsersResponse>();
        await telegramBotUserProfilePhotoService.ApplyHasProfilePhotoFlagsAsync(
            response.TelegramBotUsers, cancellationToken);
        return Ok(ApiResponse<GetAllTelegramUsersResponse>.SuccessResponse(response));
    }

    [HttpGet("profile-photo-index")]
    public async Task<ActionResult<ApiResponse<TelegramBotUserProfilePhotoIndexResponse>>> GetProfilePhotoIndex(
        CancellationToken cancellationToken)
    {
        var index = await telegramBotUserProfilePhotoService.GetPhotoIndexAsync(cancellationToken);
        return Ok(ApiResponse<TelegramBotUserProfilePhotoIndexResponse>.SuccessResponse(index));
    }

    [HttpPost("profile-photo")]
    public async Task<ActionResult<ApiResponse<UpsertTelegramBotUserProfilePhotoResponse>>> UpsertProfilePhoto(
        [FromBody] UpsertTelegramBotUserProfilePhotoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await telegramBotUserProfilePhotoService.UpsertAsync(request, cancellationToken);
        return Ok(ApiResponse<UpsertTelegramBotUserProfilePhotoResponse>.SuccessResponse(result));
    }

    [HttpGet("profile-photo-meta/{telegramId:long}")]
    public async Task<ActionResult<ApiResponse<TelegramBotUserProfilePhotoMetaResponse>>> GetProfilePhotoMeta(
        [FromRoute] long telegramId,
        CancellationToken cancellationToken)
    {
        var meta = await telegramBotUserProfilePhotoService.GetMetaByTelegramIdAsync(telegramId, cancellationToken);
        if (meta is null)
            return NotFound(ApiResponse<TelegramBotUserProfilePhotoMetaResponse>.ErrorResponse("Telegram user not found."));
        return Ok(ApiResponse<TelegramBotUserProfilePhotoMetaResponse>.SuccessResponse(meta));
    }

    [HttpGet("profile-photo-file/{telegramId:long}")]
    public async Task<IActionResult> GetProfilePhotoFile([FromRoute] long telegramId,
        CancellationToken cancellationToken)
    {
        var image = await telegramBotUserProfilePhotoService.GetImageByTelegramIdAsync(telegramId, cancellationToken);
        if (image is null)
            return NotFound();
        return File(image.Value.Bytes, image.Value.MimeType);
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