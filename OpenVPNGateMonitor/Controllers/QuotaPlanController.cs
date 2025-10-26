using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.QuotaPlans;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.QuotaPlans;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotaPlanController(IQuotaPlanService quotaPlanService) : ControllerBase
{
    /// <summary>
    /// Get all quota plans.
    /// </summary>
    [HttpPost("GetAll")]
    public async Task<ActionResult<ApiResponse<List<QuotaPlanResponse>>>> GetAll(
        [FromBody] GetQuotaPlansRequest request,
        CancellationToken ct)
    {
        var result = await quotaPlanService.GetAllAsync(ct);
        var response = result.Select(QuotaPlanResponse.FromEntity).ToList();
        return Ok(ApiResponse<List<QuotaPlanResponse>>.SuccessResponse(response));
    }

    /// <summary>
    /// Get a quota plan by id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<QuotaPlanResponse>>> GetById(int id, CancellationToken ct)
    {
        var plan = await quotaPlanService.GetByIdAsync(id, ct);
        if (plan == null)
            return NotFound(ApiResponse<QuotaPlanResponse>.ErrorResponse("Quota plan not found"));

        return Ok(ApiResponse<QuotaPlanResponse>.SuccessResponse(QuotaPlanResponse.FromEntity(plan)));
    }

    /// <summary>
    /// Create a new quota plan.
    /// </summary>
    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<QuotaPlanResponse>>> Create(
        [FromBody] CreateOrUpdateQuotaPlanRequest request,
        CancellationToken ct)
    {
        var entity = request.ToEntity();
        var created = await quotaPlanService.CreateAsync(entity, request.IsDefault, ct);
        return Ok(ApiResponse<QuotaPlanResponse>.SuccessResponse(QuotaPlanResponse.FromEntity(created)));
    }

    /// <summary>
    /// Update an existing quota plan.
    /// </summary>
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<string>>> Update(
        [FromBody] CreateOrUpdateQuotaPlanRequest request,
        CancellationToken ct)
    {
        var entity = request.ToEntity();
        await quotaPlanService.UpdateAsync(entity, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
    }

    /// <summary>
    /// Delete a quota plan by id.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(int id, CancellationToken ct)
    {
        await quotaPlanService.DeleteAsync(id, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
    }

    /// <summary>
    /// Set quota plan as default.
    /// </summary>
    [HttpPost("{id:int}/SetDefault")]
    public async Task<ActionResult<ApiResponse<string>>> SetDefault(int id, CancellationToken ct)
    {
        await quotaPlanService.SetDefaultAsync(id, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Default plan updated"));
    }
}
