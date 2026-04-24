using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTagTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-servers")]
[Authorize]
public class VpnServersController(IVpnDataService vpnDataService,
    IVpnServerOverviewQuery openVpnServerOverviewQuery, IVpnServerQueryService openVpnServerQueryService,
    IVpnServerTagQueryService openVpnServerTagQueryService,
    IOpenVpnBackgroundService openVpnBackgroundService,
    IMicroserviceInfoService microserviceInfoService,
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet("get-all-with-status")]
    public async Task<ActionResult<ApiResponse<VpnServerWithStatusesResponse>>> GetAllServersWithStatus(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        List<VpnServerWithStatusDto> result;
        if (HttpUserContext.IsPrivileged(User))
        {
            result = await openVpnServerOverviewQuery.GetAllVpnServersWithStatusAsync(
                includeDeleted, requireQuotaPlanAssignment: false, restrictToQuotaPlanId: null, ct);
        }
        else
        {
            if (!HttpUserContext.TryGetUserId(User, out var userId))
                return Unauthorized(ApiResponse<VpnServerWithStatusesResponse>.ErrorResponse("User id missing from token."));
            var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
            if (uqp is null)
                result = [];
            else
                result = await openVpnServerOverviewQuery.GetAllVpnServersWithStatusAsync(
                    includeDeleted, requireQuotaPlanAssignment: false, restrictToQuotaPlanId: uqp.QuotaPlanId, ct);
        }

        var response = result.Adapt<VpnServerWithStatusesResponse>();
        await FillTagsForOverviewResponse(response, ct);
        return Ok(ApiResponse<VpnServerWithStatusesResponse>.SuccessResponse(response));
    }

    [HttpGet("get-server-with-status/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<VpnServerWithStatusResponse>>> GetServerWithStatus(
        [FromRoute] GetServerWithStatsRequest request, CancellationToken ct)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<VpnServerWithStatusResponse>(User, vpnServerAccessQueryService,
                request.VpnServerId, ct) is { } denyStatus)
            return denyStatus;

        var serverInfo = await openVpnServerOverviewQuery.GetVpnServerWithStatusAsync(request.VpnServerId, ct);
        var response = serverInfo.Adapt<VpnServerWithStatusResponse>();
        if (response.VpnServerWithStatus?.VpnServerResponses?.VpnServer != null)
            response.VpnServerWithStatus.VpnServerResponses.VpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(request.VpnServerId, ct);
        return Ok(ApiResponse<VpnServerWithStatusResponse>.SuccessResponse(response));
    }

    [HttpGet("get-microservice-info/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<VpnMicroserviceDiagnosticsDto>>> GetMicroserviceInfo(
        [FromRoute] int vpnServerId, CancellationToken ct)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<VpnMicroserviceDiagnosticsDto>(User, vpnServerAccessQueryService,
                vpnServerId, ct) is { } denyMicro)
            return denyMicro;

        var info = await microserviceInfoService.GetInfoAsync(vpnServerId, ct);
        return Ok(ApiResponse<VpnMicroserviceDiagnosticsDto>.SuccessResponse(info));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-microservice-info-by-url")]
    public async Task<ActionResult<ApiResponse<VpnMicroserviceDiagnosticsDto>>> GetMicroserviceInfoByUrl(
        [FromQuery] string baseUrl,
        [FromQuery] VpnServerType? serverType,
        CancellationToken ct)
    {
        var info = await microserviceInfoService.GetInfoByUrlAsync(baseUrl, serverType, ct);
        if (info is null)
            return NotFound(ApiResponse<VpnMicroserviceDiagnosticsDto>.ErrorResponse("Microservice info endpoint not found (404)."));
        return Ok(ApiResponse<VpnMicroserviceDiagnosticsDto>.SuccessResponse(info));
    }

    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<VpnServersResponse>>> GetAllServers(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        List<VpnServer> serversList;
        if (HttpUserContext.IsPrivileged(User))
        {
            serversList = await openVpnServerQueryService.GetAll(includeDeleted, requireQuotaPlanAssignment: false,
                restrictToQuotaPlanId: null, ct);
        }
        else
        {
            if (!HttpUserContext.TryGetUserId(User, out var userId))
                return Unauthorized(ApiResponse<VpnServersResponse>.ErrorResponse("User id missing from token."));
            var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
            if (uqp is null)
                serversList = [];
            else
                serversList = await openVpnServerQueryService.GetAll(includeDeleted, requireQuotaPlanAssignment: false,
                    restrictToQuotaPlanId: uqp.QuotaPlanId, ct);
        }

        var response = serversList.Adapt<VpnServersResponse>();
        if (serversList.Count > 0)
        {
            var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(serversList.Select(s => s.Id).ToList(), ct);
            for (var i = 0; i < response.VpnServers.Count; i++)
                response.VpnServers[i].Tags = tagNamesByServer.GetValueOrDefault(serversList[i].Id, []);
        }
        return Ok(ApiResponse<VpnServersResponse>.SuccessResponse(response));
    }

    [HttpGet("get/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<VpnServerResponse>>> GetServer(
        [FromRoute] GetServerRequest request, CancellationToken ct)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<VpnServerResponse>(User, vpnServerAccessQueryService,
                request.VpnServerId, ct) is { } denyGet)
            return denyGet;

        var server = await openVpnServerQueryService.GetById(request.VpnServerId, ct);
        if (server == null)
            return NotFound();
        var response = server.Adapt<VpnServerResponse>();
        response.VpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(request.VpnServerId, ct);
        return Ok(ApiResponse<VpnServerResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<VpnServerResponse>>> AddServer(
        [FromBody] AddServerRequest request, CancellationToken ct)
    {
        var newServer = await vpnDataService.AddVpnServer(request.Adapt<VpnServer>(), request.QuotaPlanIds, request.TagIds, ct);
        var response = newServer.Adapt<VpnServerResponse>();
        response.VpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(newServer.Id, ct);
        return Ok(ApiResponse<VpnServerResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<VpnServerResponse>>> UpdateServer(
        [FromBody] UpdateServerRequest request, CancellationToken ct)
    {
        var updatedServer = await vpnDataService.UpdateVpnServer(request.Adapt<VpnServer>(),
            request.QuotaPlanIds, request.TagIds, ct);
        var response = updatedServer.Adapt<VpnServerResponse>();
        response.VpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(updatedServer.Id, ct);
        return Ok(ApiResponse<VpnServerResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpDelete("delete/{VpnServerId:int}")]
    //todo: fixed response
    public async Task<IActionResult> DeleteServer(
        [FromRoute] DeleteServerRequest request, CancellationToken ct)
    {
        var deletedServer = await vpnDataService.DeleteVpnServer(request.VpnServerId, ct);

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

    private async Task FillTagsForOverviewResponse(VpnServerWithStatusesResponse response, CancellationToken ct)
    {
        if (response.VpnServerWithStatuses.Count == 0) return;
        var ids = response.VpnServerWithStatuses
            .Select(x => x.VpnServerResponses.VpnServer.Id)
            .ToList();
        var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(ids, ct);
        foreach (var item in response.VpnServerWithStatuses)
        {
            var id = item.VpnServerResponses.VpnServer.Id;
            item.VpnServerResponses.VpnServer.Tags = tagNamesByServer.GetValueOrDefault(id, []);
        }
    }
}
