using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeoLiteController(
    IGeoLiteQueryService geoLiteQueryService,
    IGeoLiteUpdaterService geoLiteUpdaterService)
    : ControllerBase
{
    [HttpGet("GetDatabasePath")]
    public ActionResult<ApiResponse<GetDatabasePathResponse>> GetDatabasePath()
    {
        var response = new GetDatabasePathResponse() { DatabasePath = geoLiteQueryService.GetDatabasePath() };
        return Ok(ApiResponse<GetDatabasePathResponse>.SuccessResponse(response));
    }
    
    [HttpGet("GetGeoInfo")]
    public async Task<ActionResult<ApiResponse<GetGeoInfoResponse>>> GetGeoInfo(GetGeoInfoRequest request,
        CancellationToken cancellationToken)
    {
        var response = new GetGeoInfoResponse()
        {
            GeoInfo = await geoLiteQueryService.GetGeoInfoAsync(request.IpAddress, cancellationToken) ?? 
                      throw new InvalidOperationException("No geo information found for the provided IP address.")
        };
        
        return Ok(ApiResponse<GetGeoInfoResponse>.SuccessResponse(response));
    }
    
    [HttpGet("GetVersionDatabase")]
    public async Task<ActionResult<ApiResponse<GetVersionDatabaseResponse>>> GetVersionDatabase(
        CancellationToken cancellationToken)
    {
        var response = new GetVersionDatabaseResponse()
        {
            DatabaseVersion = await geoLiteQueryService.GetDatabaseVersionAsync(cancellationToken)
        };
        return Ok(ApiResponse<GetVersionDatabaseResponse>.SuccessResponse(response));
    }

    [HttpPost("UpdateDatabase")]
    public async Task<ActionResult<ApiResponse<GeoLiteUpdateResponse>>> UpdateDatabase(
        CancellationToken cancellationToken)
    {
        var updateResult = await geoLiteUpdaterService.DownloadAndUpdateDatabaseAsync(cancellationToken);
        return Ok(ApiResponse<GeoLiteUpdateResponse>.SuccessResponse(updateResult));
    }
    
    [HttpGet("CheckNewVersion")]
    public async Task<ActionResult<ApiResponse<GeoLiteVersionCheckResponse>>> CheckNewVersion(
        CancellationToken cancellationToken)
    {
        var checkResult = await geoLiteUpdaterService.CheckNewVersionAsync(cancellationToken);
        return Ok(ApiResponse<GeoLiteVersionCheckResponse>.SuccessResponse(checkResult));
    }
}