using System.Net.WebSockets;
using System.Text;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-servers")]
[Authorize]
public class OpenVpnServersController(IVpnDataService vpnDataService,
    IOpenVpnServerOverviewQuery openVpnServerOverviewQuery, IOpenVpnServerQueryService openVpnServerQueryService,
    IOpenVpnBackgroundService openVpnBackgroundService,
    IMicroserviceInfoService microserviceInfoService) : BaseController
{
    [HttpGet("get-all-with-status")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerWithStatusesResponse>>> GetAllServersWithStatus(
        CancellationToken ct)
    {
        var result = await openVpnServerOverviewQuery.GetAllOpenVpnServersWithStatusAsync(ct);
        return Ok(ApiResponse<OpenVpnServerWithStatusesResponse>.SuccessResponse(
            result.Adapt<OpenVpnServerWithStatusesResponse>()));
    }

    [HttpGet("get-server-with-status/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerWithStatusResponse>>> GetServerWithStatus(
        [FromRoute] GetServerWithStatsRequest request, CancellationToken ct)
    {
        var serverInfo = await openVpnServerOverviewQuery.GetOpenVpnServerWithStatusAsync(request.VpnServerId, ct);

        return Ok(ApiResponse<OpenVpnServerWithStatusResponse>.SuccessResponse(
            serverInfo.Adapt<OpenVpnServerWithStatusResponse>()));
    }

    [HttpGet("get-microservice-info/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<RootInfoResponse>>> GetMicroserviceInfo(
        [FromRoute] int vpnServerId, CancellationToken ct)
    {
        var info = await microserviceInfoService.GetInfoAsync(vpnServerId, ct);
        return Ok(ApiResponse<RootInfoResponse>.SuccessResponse(info));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-microservice-info-by-url")]
    public async Task<ActionResult<ApiResponse<RootInfoResponse>>> GetMicroserviceInfoByUrl(
        [FromQuery] string baseUrl, CancellationToken ct)
    {
        var info = await microserviceInfoService.GetInfoByUrlAsync(baseUrl, ct);
        return Ok(ApiResponse<RootInfoResponse>.SuccessResponse(info));
    }

    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<OpenVpnServersResponse>>> GetAllServers(
        CancellationToken ct)
    {
        var serversList = await openVpnServerQueryService.GetAll(ct);

        return Ok(ApiResponse<OpenVpnServersResponse>.SuccessResponse(
            serversList.Adapt<OpenVpnServersResponse>()));
    }

    [HttpGet("get/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> GetServer(
        [FromRoute] GetServerRequest request, CancellationToken ct)
    {
        var server = await openVpnServerQueryService.GetById(request.VpnServerId, ct);

        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(server.Adapt<OpenVpnServerResponse>()));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> AddServer(
        [FromBody] AddServerRequest request, CancellationToken ct)
    {
        var newServer = await vpnDataService.AddOpenVpnServer(request.Adapt<OpenVpnServer>(), request.QuotaPlanIds, ct);

        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(newServer.Adapt<OpenVpnServerResponse>()));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> UpdateServer(
        [FromBody] UpdateServerRequest request, CancellationToken ct)
    {
        var updatedServer = await vpnDataService.UpdateOpenVpnServer(request.Adapt<OpenVpnServer>(), 
            request.QuotaPlanIds, ct);

        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(
            updatedServer.Adapt<OpenVpnServerResponse>()));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpDelete("delete/{VpnServerId:int}")]
    //todo: fixed response
    public async Task<IActionResult> DeleteServer(
        [FromRoute] DeleteServerRequest request, CancellationToken ct)
    {
        var deletedServer = await vpnDataService.DeleteOpenVpnServer(request.VpnServerId, ct);

        return Ok(ApiResponse<bool>.SuccessResponse(deletedServer));
    }

    [HttpGet("status")]
    public ActionResult<ApiResponse<ServiceStatusesResponse>> GetStatus()
    {
        var serverStatuses = openVpnBackgroundService
            .GetStatus()
            .Select(kv => kv.Value.Adapt<ServiceStatusDto>())
            .ToList();

        var response = new ServiceStatusesResponse
        {
            ServiceStatuses = serverStatuses
        };

        return Ok(ApiResponse<ServiceStatusesResponse>.SuccessResponse(response));
    }

    [HttpPost("run-now")]
    public async Task<ActionResult<ApiResponse<string>>> RunNow(CancellationToken ct)
    {
        var serverStatuses = openVpnBackgroundService.GetStatus();

        if (serverStatuses.Values.All(x => x.Status != ServiceStatus.Running))
        {
            await openVpnBackgroundService.RunNow(ct);
        }

        return Ok(ApiResponse<string>.SuccessResponse("OpenVPN background task executed immediately."));
    }
}