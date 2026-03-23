using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/tgbot-incoming-message-logs")]
[Authorize]
public class TelegramBotIncomingMessageLogController(
    IIncomingMessageLogService incomingMessageLogService,
    IIncomingMessageLogQueryService incomingMessageLogQueryService) : BaseController
{
    [Authorize(Roles = "Admin,App")]
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<AddMessageResponse>>> AddMessage(
        [FromBody] AddMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await incomingMessageLogService.SaveMessageAsync(request, cancellationToken);

        return Ok(ApiResponse<AddMessageResponse>.SuccessResponse(response));
    }
    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllMessagesResponse>>> GetAllMessages(
        [FromQuery] GetAllMessagesRequest request, CancellationToken cancellationToken)
    {
        var messages = await incomingMessageLogQueryService.GetPage(
            request.Page, request.PageSize, cancellationToken);

        var response = new GetAllMessagesResponse
        {
            Messages = messages.Adapt<PagedResponse<MessageDto>>()
        };

        return Ok(ApiResponse<GetAllMessagesResponse>.SuccessResponse(response));
    }
    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-by-telegram-userid/{telegramId}")]
    public async Task<ActionResult<ApiResponse<GetByTelegramIdMessagesResponse>>> GetByTelegramUserId(
        [FromRoute] long telegramId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var messages = await incomingMessageLogQueryService.GetPageByTelegramId(telegramId, page, pageSize, ct);

        var response = new GetByTelegramIdMessagesResponse
        {
            Messages = messages.Adapt<PagedResponse<MessageDto>>()
        };

        return Ok(ApiResponse<GetByTelegramIdMessagesResponse>.SuccessResponse(response));
    }
    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-by-id")]
    public async Task<ActionResult<ApiResponse<GetByIdMessageResponse>>> GetById(
        [FromRoute] GetByIdMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await incomingMessageLogService.GetByIdAsync(request.Id, cancellationToken);
        if (message is null)
            return NotFound(ApiResponse<GetByIdMessageResponse>.ErrorResponse("Message not found"));

        return Ok(ApiResponse<GetByIdMessageResponse>.SuccessResponse(
            message.Adapt<GetByIdMessageResponse>()));
    }
}