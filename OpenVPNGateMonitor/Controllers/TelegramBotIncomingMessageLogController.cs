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
    
    // [HttpGet("GetAllMessages")]
    // public async Task<ActionResult<ApiResponse<AddMessageResponse>>> GetAllMessages(
    //     [FromBody] AddMessageRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     var response = await incomingMessageLogService.GetAllAsync();
    //
    //     return Ok(ApiResponse<AddMessageResponse>.SuccessResponse(response));
    // }
    
    // [HttpGet("GetByTelegramUserId")]
    // public async Task<ActionResult<ApiResponse<AddMessageResponse>>> GetAllMessages(
    //     [FromBody] AddMessageRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     var response = await incomingMessageLogService.GetByTelegramIdAsync
    //
    //     return Ok(ApiResponse<AddMessageResponse>.SuccessResponse(response));
    // }
    //
    // [HttpGet("GetById")]
    // public async Task<ActionResult<ApiResponse<AddMessageResponse>>> GetAllMessages(
    //     [FromBody] AddMessageRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     var response = await incomingMessageLogService.GetByIdAsync();
    //
    //     return Ok(ApiResponse<AddMessageResponse>.SuccessResponse(response));
    // }
}