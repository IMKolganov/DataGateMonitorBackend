using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class UserController(
    IUserService userService,
    IUserMergeService userMergeService,
    ITelegramAccountLinkService telegramAccountLinkService,
    IFreeTierAccessComplianceService freeTierAccessComplianceService,
    ICurrentUserService currentUserService) : BaseController
{
    [HttpPost("register-from-tgbot")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> RegisterUser([FromBody] RegisterUserFromTgBotRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await userService.RegisterUserFromTgBot(request.Adapt<RegisterUserFromTgBotRequest>(), 
            cancellationToken);
        
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(response.Adapt<UsersResponse>()));
    }
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllUsersResponse>>> GetAllUsers(
        [FromQuery] GetAllUsersRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.GetUsersPage(request, cancellationToken);
        return Ok(ApiResponse<GetAllUsersResponse>.SuccessResponse(response));
    }
    
    [HttpGet("get-by-id/{id:int}")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> GetUserById([FromRoute]GetUserByIdRequest request, 
        CancellationToken cancellationToken)
    {
        var telegramBotUsers = await userService.GetUserById(request, cancellationToken);
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(telegramBotUsers.Adapt<UsersResponse>()));
    }
    
    [HttpGet("get-by-external-id/{externalId:int}")]
    public async Task<ActionResult<ApiResponse<UsersResponse>>> GetUserByExternalId(
        [FromRoute]GetUserByExternalIdRequest request, CancellationToken cancellationToken)
    {
        var telegramBotUsers = await userService.GetUserByExternalId(request, cancellationToken);
        return Ok(ApiResponse<UsersResponse>.SuccessResponse(telegramBotUsers.Adapt<UsersResponse>()));
    }

    [HttpGet("email-confirmation-status/{id:int}")]
    public async Task<ActionResult<ApiResponse<GetUserEmailConfirmationStatusResponse>>> GetEmailConfirmationStatus(
        [FromRoute] GetUserEmailConfirmationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.GetEmailConfirmationStatus(request, cancellationToken);
        return Ok(ApiResponse<GetUserEmailConfirmationStatusResponse>.SuccessResponse(response));
    }

    [HttpPost("confirm-email/{id:int}")]
    public async Task<ActionResult<ApiResponse<ConfirmUserEmailResponse>>> ConfirmEmail(
        [FromRoute] ConfirmUserEmailRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.ConfirmEmailManually(request, cancellationToken);
        return Ok(ApiResponse<ConfirmUserEmailResponse>.SuccessResponse(response));
    }

    /// <summary>
    /// Merges a Google-login user into a Telegram-login user. Google account is archived then removed.
    /// Use <paramref name="request"/>.DryRun to preview counts without writing.
    /// </summary>
    [HttpPost("merge-telegram-google")]
    [ProducesResponseType(typeof(ApiResponse<MergeTelegramGoogleUsersResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MergeTelegramGoogleUsersResponse>>> MergeTelegramGoogle(
        [FromBody] MergeTelegramGoogleUsersRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userMergeService.MergeTelegramGoogleAsync(
            request,
            currentUserService.UserId,
            cancellationToken);

        return Ok(ApiResponse<MergeTelegramGoogleUsersResponse>.SuccessResponse(response));
    }

    /// <summary>
    /// Telegram bot: user entered a link code from the client app. Merges dashboard account into Telegram user.
    /// </summary>
    [HttpPost("merge-telegram-google/by-link-code")]
    [ProducesResponseType(typeof(ApiResponse<CompleteTelegramAccountLinkResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CompleteTelegramAccountLinkResponse>>> MergeTelegramGoogleByLinkCode(
        [FromBody] CompleteTelegramAccountLinkRequest request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<CompleteTelegramAccountLinkResponse>.ErrorResponse("Code is required."));

        if (request.TelegramId <= 0)
            return BadRequest(ApiResponse<CompleteTelegramAccountLinkResponse>.ErrorResponse("TelegramId is required."));

        var result = await telegramAccountLinkService.CompleteLinkByCodeAsync(
            request.Code,
            request.TelegramId,
            ct);

        if (!result.Success)
            return BadRequest(ApiResponse<CompleteTelegramAccountLinkResponse>.ErrorResponse(result.Message));

        return Ok(ApiResponse<CompleteTelegramAccountLinkResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Audits whether a Telegram user with Free/Default plan is merged or subscribed to the required channel.
    /// Notifies admins when the check fails. Optional channelSubscribed comes from the bot getChatMember result.
    /// </summary>
    [HttpPost("audit-free-tier-access/by-telegram/{telegramId:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> AuditFreeTierAccessByTelegram(
        [FromRoute] long telegramId,
        [FromQuery] bool? channelSubscribed,
        [FromQuery] string? context,
        CancellationToken cancellationToken)
    {
        var result = await freeTierAccessComplianceService.AuditAndNotifyIfNeededByTelegramIdAsync(
            telegramId,
            string.IsNullOrWhiteSpace(context) ? "Telegram bot audit" : context,
            channelSubscribed,
            cancellationToken);

        return Ok(ApiResponse<bool>.SuccessResponse(result.IsCompliant));
    }
}