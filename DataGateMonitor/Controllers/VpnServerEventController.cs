using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerEventLogTable;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-events")]
[Authorize(Roles = "Admin,App")]
public class VpnServerEventController(
    IVpnServerEventLogQueryService openVpnServerEventLogQueryService,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService,
    IOpenVpnEventClientFactory eventClientFactory) : BaseController
{
    /// <summary>
    /// Paged events by VPN server id.
    /// </summary>
    [HttpGet("get-by-server")]
    public async Task<ActionResult<ApiResponse<VpnServerEventResponse>>> GetEventByVpnServerId(
        [FromQuery] GetVpnServerEventRequest request,
        CancellationToken cancellationToken)
    {
        var commonNames = await ResolveCommonNamesAsync(request, cancellationToken);

        var page = await openVpnServerEventLogQueryService.GetByVpnServerId(
            request.VpnServerId,
            request.Page,
            request.PageSize,
            cancellationToken,
            commonNames,
            request.EventType);

        var dto = page.Adapt<VpnServerEventResponse>();
        return Ok(ApiResponse<VpnServerEventResponse>.SuccessResponse(dto));
    }

    /// <summary>
    /// Distinct client app versions (IV_GUI_VER) seen on connect events for a user or CN on a server.
    /// </summary>
    [HttpGet("app-versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VpnClientAppVersionSummaryItemDto>>>> GetAppVersions(
        [FromQuery] GetVpnClientAppVersionsRequest request,
        CancellationToken cancellationToken)
    {
        var commonNames = await ResolveCommonNamesAsync(
            request.VpnServerId,
            request.CommonName,
            request.ExternalId,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.ExternalId?.Trim()) && commonNames is not { Count: > 0 })
            return Ok(ApiResponse<IReadOnlyList<VpnClientAppVersionSummaryItemDto>>.SuccessResponse([]));

        var items = await openVpnServerEventLogQueryService.GetAppVersionSummaryAsync(
            request.VpnServerId,
            commonNames,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<VpnClientAppVersionSummaryItemDto>>.SuccessResponse(items));
    }

    /// <summary>
    /// Returns status snapshots for all cached OpenVPN event clients.
    /// </summary>
    [HttpGet("status")]
    public ActionResult<ApiResponse<ConnectionStatusesResponse>> GetAllClientStatuses()
    {
        var statuses = eventClientFactory.GetAllClientStatuses();
        return Ok(ApiResponse<ConnectionStatusesResponse>.SuccessResponse(statuses));
    }

    /// <summary>
    /// Returns status snapshot for a single server id (404 if not found in cache).
    /// </summary>
    [HttpGet("status/{vpnServerId:int}")]
    public ActionResult<ApiResponse<ConnectionStatusResponse>> GetClientStatus([FromRoute]
        GetClientStatusRequest request)
    {
        if (eventClientFactory.TryGetClientStatus(request.VpnServerId, out var status) && status is not null)
            return Ok(ApiResponse<ConnectionStatusResponse>.SuccessResponse(status));

        return NotFound(ApiResponse<ConnectionStatusResponse>.ErrorResponse(
            $"No cached client found for serverId={request.VpnServerId}"));
    }

    private Task<IReadOnlyList<string>?> ResolveCommonNamesAsync(
        GetVpnServerEventRequest request,
        CancellationToken cancellationToken)
        => ResolveCommonNamesAsync(
            request.VpnServerId,
            request.CommonName,
            request.ExternalId,
            cancellationToken);

    private async Task<IReadOnlyList<string>?> ResolveCommonNamesAsync(
        int vpnServerId,
        string? commonName,
        string? externalId,
        CancellationToken cancellationToken)
    {
        var cn = commonName?.Trim();
        if (!string.IsNullOrWhiteSpace(cn))
            return [cn!];

        var ext = externalId?.Trim();
        if (string.IsNullOrWhiteSpace(ext))
            return null;

        var files = await issuedOvpnFileQueryService.GetAllByExternalId(ext, cancellationToken);
        var names = files
            .Where(f => f.VpnServerId == vpnServerId)
            .Select(f => f.CommonName)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return names.Count == 0 ? null : names;
    }
}
