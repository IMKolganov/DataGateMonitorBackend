using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 applications with Page/PageSize + PagedResponse.</summary>
[Route("api/v2/applications")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ApplicationsV2Controller(IApplicationService appService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ApplicationsV2Response>>> GetPage(
        [FromQuery] GetApplicationsV2Request request,
        CancellationToken cancellationToken)
    {
        var filter = new GetAllApplicationsRequest
        {
            Name = request.Name,
            ClientId = request.ClientId,
            IsRevoked = request.IsRevoked,
        };

        var apps = await appService.GetAllApplicationsAsync(filter, cancellationToken);
        var dtoList = apps.Adapt<List<ApplicationDto>>();

        return Ok(ApiResponse<ApplicationsV2Response>.SuccessResponse(new ApplicationsV2Response
        {
            Applications = PagedResponseFactory.FromItems(dtoList, request.Page, request.PageSize)
        }));
    }
}
