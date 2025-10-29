using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[Route("api/applications")]
[ApiController]
[Authorize]
public class ApplicationsController(IApplicationService appService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterApplicationResponse>>> RegisterApplication([FromBody] 
        RegisterApplicationRequest request, 
        CancellationToken cancellationToken)
    {
        var newApp = await appService.RegisterApplicationAsync(request.Name, cancellationToken);
        
        return Ok(ApiResponse<RegisterApplicationResponse>.SuccessResponse(
            newApp.Adapt<RegisterApplicationResponse>()));
    }

    //todo: update shared models
    // [HttpGet("get-all")]
    // public async Task<ActionResult<ApiResponse<ApplicationsResponse>>> GetAllApplications(
    // CancellationToken cancellationToken)
    // {
    //     var apps = await appService.GetAllApplicationsAsync(cancellationToken);
    //     return Ok(ApiResponse<ApplicationsResponse>.SuccessResponse(apps.Adapt<ApplicationsResponse>()));
    // }

    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse<string>>> RevokeApplication(
        [FromBody] RevokeApplicationRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await appService.RevokeApplicationAsync(request.ClientId, cancellationToken);
        
        if (!result)
            return NotFound(ApiResponse<string>.ErrorResponse("Application not found"));

        return Ok(ApiResponse<string>.SuccessResponse("Application revoked"));
    }
}