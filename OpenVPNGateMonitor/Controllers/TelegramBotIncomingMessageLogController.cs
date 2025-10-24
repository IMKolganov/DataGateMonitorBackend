using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TelegramBotIncomingMessageLogController(
    IIncomingMessageLogService incomingMessageLogService) : ControllerBase
{
    [HttpPost("AddMessage")]
    public async Task<ActionResult<ApiResponse<AddMessageResponse>>> AddMessage(
        [FromBody] AddMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await incomingMessageLogService.SaveMessageAsync(request, cancellationToken);

        return Ok(ApiResponse<AddMessageResponse>.SuccessResponse(response));
    }
    
    [HttpGet("GetAllMessages")]
    public async Task<ActionResult<ApiResponse<GetAllMessagesResponse>>> GetAllMessages(
        CancellationToken cancellationToken)
    {
        var response = await incomingMessageLogService.GetAllAsync(cancellationToken);
    
        return Ok(ApiResponse<GetAllMessagesResponse>.SuccessResponse(
            response.Adapt<GetAllMessagesResponse>()));
    }
    
    [HttpGet("GetByTelegramUserId")]
    public async Task<ActionResult<ApiResponse<GetByTelegramIdMessagesResponse>>> GetAllMessages(
        [FromBody] GetAllByTelegramIdMessagesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await incomingMessageLogService.GetByTelegramIdAsync(request.TelegramId, 
            cancellationToken);
    
        return Ok(ApiResponse<GetByTelegramIdMessagesResponse>.SuccessResponse(
            response.Adapt<GetByTelegramIdMessagesResponse>()));
    }
    
    [HttpGet("GetById")]
    public async Task<ActionResult<ApiResponse<GetByIdMessageResponse>>> GetById(
        [FromBody] GetByIdMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await incomingMessageLogService.GetByIdAsync(request.Id, cancellationToken);
    
        return Ok(ApiResponse<GetByIdMessageResponse>.SuccessResponse(
            response.Adapt<GetByIdMessageResponse>()));
    }
}