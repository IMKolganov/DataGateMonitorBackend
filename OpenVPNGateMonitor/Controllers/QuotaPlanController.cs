using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.QuotaPlans;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/quota-plans")]
[Authorize]
public class QuotaPlanController(IQuotaPlanService quotaPlanService) : BaseController
{
    /// <summary>Get all quota plans.</summary>
    [Authorize(Roles = "Admin,App,VpnUser")]
    [HttpPost("get-all")]
    public async Task<ActionResult<ApiResponse<QuotaPlansResponse>>> GetAll(
        [FromBody] GetQuotaPlansRequest request,
        CancellationToken ct)
    {
        var entities = await quotaPlanService.GetAllAsync(ct);
    
        if (!request.IncludeInactive)
            entities = entities.Where(x => x.IsActive).ToList();
    
        var dto = entities.Adapt<QuotaPlansResponse>();
        return Ok(ApiResponse<QuotaPlansResponse>.SuccessResponse(dto));
    }

    /// <summary>Get a quota plan by id.</summary>
    [Authorize(Roles = "Admin,App,VpnUser")]
    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<ApiResponse<QuotaPlanResponse>>> GetById(int id, CancellationToken ct)
    {
        var entity = await quotaPlanService.GetByIdAsync(id, ct);
        if (entity == null)
            return NotFound(ApiResponse<QuotaPlanResponse>.ErrorResponse("Quota plan not found"));

        var dto = entity.Adapt<QuotaPlanResponse>();
        return Ok(ApiResponse<QuotaPlanResponse>.SuccessResponse(dto));
    }

    /// <summary>Create a new quota plan.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<QuotaPlanResponse>>> Create(
        [FromBody] CreateOrUpdateQuotaPlanRequest request,
        CancellationToken ct)
    {
        var entity = request.Adapt<QuotaPlan>();
        var created = await quotaPlanService.CreateAsync(entity, request.IsDefault, ct);

        var dto = new QuotaPlanResponse
        {
            QuotaPlan = created.Adapt<QuotaPlanDto>()
        };

        return Ok(ApiResponse<QuotaPlanResponse>.SuccessResponse(dto));
    }

    /// <summary>Update an existing quota plan.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        [FromBody] CreateOrUpdateQuotaPlanRequest request,
        CancellationToken ct)
    {
        var entity = request.Adapt<QuotaPlan>();
        await quotaPlanService.UpdateAsync(entity, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
    }

    /// <summary>Delete a quota plan by id.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpDelete("delete/{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(int id, CancellationToken ct)
    {
        await quotaPlanService.DeleteAsync(id, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
    }

    /// <summary>Set quota plan as default.</summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPost("set-default/{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> SetDefault(int id, CancellationToken ct)
    {
        await quotaPlanService.SetDefaultAsync(id, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Default plan updated"));
    }
}
