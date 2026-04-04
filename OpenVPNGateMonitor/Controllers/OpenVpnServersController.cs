using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;
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
    IOpenVpnServerTagQueryService openVpnServerTagQueryService,
    IOpenVpnBackgroundService openVpnBackgroundService,
    IMicroserviceInfoService microserviceInfoService,
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet("get-all-with-status")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerWithStatusesResponse>>> GetAllServersWithStatus(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        List<OpenVpnServerWithStatusDto> result;
        if (HttpUserContext.IsPrivileged(User))
        {
            result = await openVpnServerOverviewQuery.GetAllOpenVpnServersWithStatusAsync(
                includeDeleted, requireQuotaPlanAssignment: false, restrictToQuotaPlanId: null, ct);
        }
        else
        {
            if (!HttpUserContext.TryGetUserId(User, out var userId))
                return Unauthorized(ApiResponse<OpenVpnServerWithStatusesResponse>.ErrorResponse("User id missing from token."));
            var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
            if (uqp is null)
                result = [];
            else
                result = await openVpnServerOverviewQuery.GetAllOpenVpnServersWithStatusAsync(
                    includeDeleted, requireQuotaPlanAssignment: false, restrictToQuotaPlanId: uqp.QuotaPlanId, ct);
        }

        var response = result.Adapt<OpenVpnServerWithStatusesResponse>();
        await FillTagsForOverviewResponse(response, ct);
        return Ok(ApiResponse<OpenVpnServerWithStatusesResponse>.SuccessResponse(response));
    }

    [HttpGet("get-server-with-status/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerWithStatusResponse>>> GetServerWithStatus(
        [FromRoute] GetServerWithStatsRequest request, CancellationToken ct)
    {
        if (await OpenVpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OpenVpnServerWithStatusResponse>(User, vpnServerAccessQueryService,
                request.VpnServerId, ct) is { } denyStatus)
            return denyStatus;

        var serverInfo = await openVpnServerOverviewQuery.GetOpenVpnServerWithStatusAsync(request.VpnServerId, ct);
        var response = serverInfo.Adapt<OpenVpnServerWithStatusResponse>();
        if (response.OpenVpnServerWithStatus?.OpenVpnServerResponses?.OpenVpnServer != null)
            response.OpenVpnServerWithStatus.OpenVpnServerResponses.OpenVpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(request.VpnServerId, ct);
        return Ok(ApiResponse<OpenVpnServerWithStatusResponse>.SuccessResponse(response));
    }

    [HttpGet("get-microservice-info/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<RootInfoResponse>>> GetMicroserviceInfo(
        [FromRoute] int vpnServerId, CancellationToken ct)
    {
        if (await OpenVpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<RootInfoResponse>(User, vpnServerAccessQueryService,
                vpnServerId, ct) is { } denyMicro)
            return denyMicro;

        var info = await microserviceInfoService.GetInfoAsync(vpnServerId, ct);
        return Ok(ApiResponse<RootInfoResponse>.SuccessResponse(info));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpGet("get-microservice-info-by-url")]
    public async Task<ActionResult<ApiResponse<RootInfoResponse>>> GetMicroserviceInfoByUrl(
        [FromQuery] string baseUrl, CancellationToken ct)
    {
        var info = await microserviceInfoService.GetInfoByUrlAsync(baseUrl, ct);
        if (info is null)
            return NotFound(ApiResponse<RootInfoResponse>.ErrorResponse("Microservice info endpoint not found (404)."));
        return Ok(ApiResponse<RootInfoResponse>.SuccessResponse(info));
    }

    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<OpenVpnServersResponse>>> GetAllServers(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        List<OpenVpnServer> serversList;
        if (HttpUserContext.IsPrivileged(User))
        {
            serversList = await openVpnServerQueryService.GetAll(includeDeleted, requireQuotaPlanAssignment: false,
                restrictToQuotaPlanId: null, ct);
        }
        else
        {
            if (!HttpUserContext.TryGetUserId(User, out var userId))
                return Unauthorized(ApiResponse<OpenVpnServersResponse>.ErrorResponse("User id missing from token."));
            var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
            if (uqp is null)
                serversList = [];
            else
                serversList = await openVpnServerQueryService.GetAll(includeDeleted, requireQuotaPlanAssignment: false,
                    restrictToQuotaPlanId: uqp.QuotaPlanId, ct);
        }

        var response = serversList.Adapt<OpenVpnServersResponse>();
        if (serversList.Count > 0)
        {
            var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(serversList.Select(s => s.Id).ToList(), ct);
            for (var i = 0; i < response.OpenVpnServers.Count; i++)
                response.OpenVpnServers[i].Tags = tagNamesByServer.GetValueOrDefault(serversList[i].Id, []);
        }
        return Ok(ApiResponse<OpenVpnServersResponse>.SuccessResponse(response));
    }

    [HttpGet("get/{VpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> GetServer(
        [FromRoute] GetServerRequest request, CancellationToken ct)
    {
        if (await OpenVpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OpenVpnServerResponse>(User, vpnServerAccessQueryService,
                request.VpnServerId, ct) is { } denyGet)
            return denyGet;

        var server = await openVpnServerQueryService.GetById(request.VpnServerId, ct);
        if (server == null)
            return NotFound();
        var response = server.Adapt<OpenVpnServerResponse>();
        response.OpenVpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(request.VpnServerId, ct);
        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> AddServer(
        [FromBody] AddServerRequest request, CancellationToken ct)
    {
        var newServer = await vpnDataService.AddOpenVpnServer(request.Adapt<OpenVpnServer>(), request.QuotaPlanIds, request.TagIds, ct);
        var response = newServer.Adapt<OpenVpnServerResponse>();
        response.OpenVpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(newServer.Id, ct);
        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> UpdateServer(
        [FromBody] UpdateServerRequest request, CancellationToken ct)
    {
        var updatedServer = await vpnDataService.UpdateOpenVpnServer(request.Adapt<OpenVpnServer>(),
            request.QuotaPlanIds, request.TagIds, ct);
        var response = updatedServer.Adapt<OpenVpnServerResponse>();
        response.OpenVpnServer.Tags = await openVpnServerTagQueryService.GetTagNamesByVpnServerId(updatedServer.Id, ct);
        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(response));
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

    private async Task FillTagsForOverviewResponse(OpenVpnServerWithStatusesResponse response, CancellationToken ct)
    {
        if (response.OpenVpnServerWithStatuses.Count == 0) return;
        var ids = response.OpenVpnServerWithStatuses
            .Select(x => x.OpenVpnServerResponses.OpenVpnServer.Id)
            .ToList();
        var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(ids, ct);
        foreach (var item in response.OpenVpnServerWithStatuses)
        {
            var id = item.OpenVpnServerResponses.OpenVpnServer.Id;
            item.OpenVpnServerResponses.OpenVpnServer.Tags = tagNamesByServer.GetValueOrDefault(id, []);
        }
    }
}
