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
    [Authorize(Roles = "Admin")]
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<AddMessageResponse>>> AddMessage(
        [FromBody] AddMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await incomingMessageLogService.SaveMessageAsync(request, cancellationToken);

        return Ok(ApiResponse<AddMessageResponse>.SuccessResponse(response));
    }
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    [HttpGet("get-by-telegram-userid/{telegramId}")]
    public async Task<ActionResult<ApiResponse<GetByTelegramIdMessagesResponse>>> GetAllMessages(
        [FromRoute] GetAllByTelegramIdMessagesRequest request, CancellationToken ct)
    {
        var messages= await incomingMessageLogQueryService.GetPageByTelegramId(
            request.TelegramId, request.Page, request.PageSize, ct);
        
        var response = new GetByTelegramIdMessagesResponse
        {
            Messages = messages.Adapt<PagedResponse<MessageDto>>()
        };

        return Ok(ApiResponse<GetByTelegramIdMessagesResponse>.SuccessResponse(response));
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("get-by-id")]
    public async Task<ActionResult<ApiResponse<GetByIdMessageResponse>>> GetById(
        [FromRoute] GetByIdMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await incomingMessageLogService.GetByIdAsync(request.Id, cancellationToken);
    
        return Ok(ApiResponse<GetByIdMessageResponse>.SuccessResponse(
            response.Adapt<GetByIdMessageResponse>()));
    }
}