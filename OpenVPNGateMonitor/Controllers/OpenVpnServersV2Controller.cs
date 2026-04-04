using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

/// <summary>
/// API v2: all servers (non-deleted) with quota-plan groups and <see cref="OpenVpnServerV2Dto.IsAccessibleForUserQuotaPlan"/>.
/// Admin/App see every server with accessibility true; other users see all servers but only their plan's servers are marked accessible.
/// </summary>
[ApiController]
[Route("api/v2/open-vpn-servers")]
[Authorize]
public class OpenVpnServersV2Controller(
    IOpenVpnServerOverviewQuery openVpnServerOverviewQuery,
    IOpenVpnServerQueryService openVpnServerQueryService,
    IOpenVpnServerQuotaPlanGroupsQuery quotaPlanGroupsQuery,
    IOpenVpnServerTagQueryService openVpnServerTagQueryService,
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService) : BaseController
{
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<OpenVpnServersV2Response>>> GetAllServers(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var serversList = await openVpnServerQueryService.GetAll(includeDeleted, requireQuotaPlanAssignment: false,
            restrictToQuotaPlanId: null, ct);
        var response = new OpenVpnServersV2Response();
        if (serversList.Count == 0)
            return Ok(ApiResponse<OpenVpnServersV2Response>.SuccessResponse(response));

        var ids = serversList.Select(s => s.Id).ToList();
        var groups = await quotaPlanGroupsQuery.GetGroupsByVpnServerIdsAsync(ids, ct);
        var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(ids, ct);

        var (allowedSet, hasUserContext) = await ResolveAccessibleServerIdsAsync(ct);
        if (!hasUserContext)
            return Unauthorized(ApiResponse<OpenVpnServersV2Response>.ErrorResponse("User id missing from token."));

        foreach (var server in serversList)
        {
            var dto = server.Adapt<OpenVpnServerDto>();
            dto.Tags = tagNamesByServer.GetValueOrDefault(server.Id, []);
            var v2 = dto.Adapt<OpenVpnServerV2Dto>();
            v2.QuotaPlanGroups = groups.GetValueOrDefault(server.Id, []);
            v2.IsAccessibleForUserQuotaPlan = allowedSet is null || allowedSet.Contains(server.Id);
            response.OpenVpnServers.Add(v2);
        }

        return Ok(ApiResponse<OpenVpnServersV2Response>.SuccessResponse(response));
    }

    [HttpGet("get-all-with-status")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerWithStatusesV2Response>>> GetAllServersWithStatus(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var result = await openVpnServerOverviewQuery.GetAllOpenVpnServersWithStatusAsync(
            includeDeleted, requireQuotaPlanAssignment: false, restrictToQuotaPlanId: null, ct);
        var response = new OpenVpnServerWithStatusesV2Response();

        if (result.Count == 0)
            return Ok(ApiResponse<OpenVpnServerWithStatusesV2Response>.SuccessResponse(response));

        var (allowedSet, hasUserContext) = await ResolveAccessibleServerIdsAsync(ct);
        if (!hasUserContext)
            return Unauthorized(ApiResponse<OpenVpnServerWithStatusesV2Response>.ErrorResponse("User id missing from token."));

        var ids = result.Select(x => x.OpenVpnServerResponses.OpenVpnServer.Id).ToList();
        var groups = await quotaPlanGroupsQuery.GetGroupsByVpnServerIdsAsync(ids, ct);
        var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(ids, ct);

        foreach (var item in result)
        {
            var id = item.OpenVpnServerResponses.OpenVpnServer.Id;
            item.OpenVpnServerResponses.OpenVpnServer.Tags = tagNamesByServer.GetValueOrDefault(id, []);
            var v2 = new OpenVpnServerWithStatusV2Dto
            {
                OpenVpnServerResponses = new OpenVpnServerV2Response
                {
                    OpenVpnServer = item.OpenVpnServerResponses.OpenVpnServer.Adapt<OpenVpnServerV2Dto>()
                },
                OpenVpnServerStatusLogResponse = item.OpenVpnServerStatusLogResponse,
                CountConnectedClients = item.CountConnectedClients,
                CountSessions = item.CountSessions,
                TotalBytesIn = item.TotalBytesIn,
                TotalBytesOut = item.TotalBytesOut
            };
            v2.OpenVpnServerResponses.OpenVpnServer.QuotaPlanGroups = groups.GetValueOrDefault(id, []);
            v2.OpenVpnServerResponses.OpenVpnServer.IsAccessibleForUserQuotaPlan =
                allowedSet is null || allowedSet.Contains(id);
            response.OpenVpnServerWithStatuses.Add(v2);
        }

        return Ok(ApiResponse<OpenVpnServerWithStatusesV2Response>.SuccessResponse(response));
    }

    /// <returns>Privileged: (null, true). Non-privileged: (allowed ids for user's plan, true). Missing user id: (_, false).</returns>
    private async Task<(HashSet<int>? AllowedServerIds, bool HasUserContext)> ResolveAccessibleServerIdsAsync(
        CancellationToken ct)
    {
        if (HttpUserContext.IsPrivileged(User))
            return (null, true);
        if (!HttpUserContext.TryGetUserId(User, out var userId))
            return (null, false);
        var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
        if (uqp is null)
            return (new HashSet<int>(), true);
        var set = await quotaPlanAllowedServerQueryService.GetVpnServerIdsByQuotaPlanId(uqp.QuotaPlanId, ct);
        return (set, true);
    }
}

