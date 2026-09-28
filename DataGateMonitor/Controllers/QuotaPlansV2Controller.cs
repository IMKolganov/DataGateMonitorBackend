using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.QuotaPlans;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 quota plans with Page/PageSize + PagedResponse.</summary>
[ApiController]
[Route("api/v2/quota-plans")]
[Authorize]
public class QuotaPlansV2Controller(IQuotaPlanService quotaPlanService) : BaseController
{
    [Authorize(Roles = "Admin,App,VpnUser")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QuotaPlansV2Response>>> GetPage(
        [FromQuery] GetQuotaPlansV2Request request,
        CancellationToken ct)
    {
        var entities = await quotaPlanService.GetAllAsync(ct);
        if (!request.IncludeInactive)
            entities = entities.Where(x => x.IsActive).ToList();

        var dtos = entities.Adapt<List<QuotaPlanDto>>();
        var response = new QuotaPlansV2Response
        {
            QuotaPlans = PagedResponseFactory.FromItems(dtos, request.Page, request.PageSize)
        };

        return Ok(ApiResponse<QuotaPlansV2Response>.SuccessResponse(response));
    }
}
