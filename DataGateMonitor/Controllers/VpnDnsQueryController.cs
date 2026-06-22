using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.VpnDnsQueryLogTable;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/vpn-dns-queries")]
[Authorize(Roles = "Admin")]
public class VpnDnsQueryController(IVpnDnsQueryLogQueryService queryService) : BaseController
{
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<VpnDnsQueryPageResponse>>> Search(
        [FromQuery] GetVpnDnsQueryRequest request,
        CancellationToken cancellationToken)
    {
        var page = await queryService.SearchAsync(request, cancellationToken);
        var response = new VpnDnsQueryPageResponse
        {
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            Items = page.Items.Adapt<List<DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto.VpnDnsQueryLogDto>>()
        };

        return Ok(ApiResponse<VpnDnsQueryPageResponse>.SuccessResponse(response));
    }
}
