using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.AdminEmail;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/admin/email-broadcast")]
[Authorize(Roles = "Admin,App")]
public class AdminEmailBroadcastController(
    IAdminEmailBroadcastService broadcastService,
    IAdminEmailTemplateService templateService) : BaseController
{
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<GetSentEmailHistoryResponse>>> GetHistory(
        [FromQuery] GetSentEmailHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var data = await broadcastService.GetHistoryAsync(request, cancellationToken);
        return Ok(ApiResponse<GetSentEmailHistoryResponse>.SuccessResponse(data));
    }

    [HttpPost("send")]
    public async Task<ActionResult<ApiResponse<SendAdminEmailResponse>>> Send(
        [FromBody] SendAdminEmailRequest request,
        CancellationToken cancellationToken)
    {
        int? sentBy = null;
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idStr, out var uid))
            sentBy = uid;

        try
        {
            var data = await broadcastService.SendAsync(request, sentBy, cancellationToken);
            return Ok(ApiResponse<SendAdminEmailResponse>.SuccessResponse(data));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SendAdminEmailResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<GetEmailTemplatesResponse>>> ListTemplates(
        CancellationToken cancellationToken)
    {
        var data = await templateService.ListSummariesAsync(cancellationToken);
        return Ok(ApiResponse<GetEmailTemplatesResponse>.SuccessResponse(data));
    }

    [HttpGet("templates/{id:int}")]
    public async Task<ActionResult<ApiResponse<EmailBroadcastTemplateDto>>> GetTemplate(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var dto = await templateService.GetByIdAsync(id, cancellationToken);
        if (dto == null)
            return NotFound(ApiResponse<EmailBroadcastTemplateDto>.ErrorResponse("Template not found."));
        return Ok(ApiResponse<EmailBroadcastTemplateDto>.SuccessResponse(dto));
    }

    [HttpPost("templates")]
    public async Task<ActionResult<ApiResponse<EmailBroadcastTemplateDto>>> CreateTemplate(
        [FromBody] CreateEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        int? createdBy = null;
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idStr, out var uid))
            createdBy = uid;

        try
        {
            var dto = await templateService.CreateAsync(request, createdBy, cancellationToken);
            return Ok(ApiResponse<EmailBroadcastTemplateDto>.SuccessResponse(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<EmailBroadcastTemplateDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("templates/{id:int}")]
    public async Task<ActionResult<ApiResponse<EmailBroadcastTemplateDto>>> UpdateTemplate(
        [FromRoute] int id,
        [FromBody] UpdateEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await templateService.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResponse<EmailBroadcastTemplateDto>.SuccessResponse(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<EmailBroadcastTemplateDto>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<EmailBroadcastTemplateDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("templates/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTemplate(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await templateService.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
