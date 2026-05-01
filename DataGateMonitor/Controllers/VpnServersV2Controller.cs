using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTagTable;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Services.Api;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>
/// API v2: all servers (non-deleted) with quota-plan groups and <see cref="VpnServerV2Dto.IsAccessibleForUserQuotaPlan"/>.
/// Admin/App see every server with accessibility true; other users see all servers but only their plan's servers are marked accessible.
/// </summary>
[ApiController]
[Route("api/v2/open-vpn-servers")]
[Authorize]
public class VpnServersV2Controller(
    IVpnServerOverviewQuery openVpnServerOverviewQuery,
    IVpnServerQueryService openVpnServerQueryService,
    IVpnServerQuotaPlanGroupsQuery quotaPlanGroupsQuery,
    IVpnServerTagQueryService openVpnServerTagQueryService,
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService) : BaseController
{
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<VpnServersV2Response>>> GetAllServers(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var serversList = await openVpnServerQueryService.GetAll(includeDeleted, requireQuotaPlanAssignment: false,
            restrictToQuotaPlanId: null, ct);
        var response = new VpnServersV2Response();
        if (serversList.Count == 0)
            return Ok(ApiResponse<VpnServersV2Response>.SuccessResponse(response));

        var ids = serversList.Select(s => s.Id).ToList();
        var groups = await quotaPlanGroupsQuery.GetGroupsByVpnServerIdsAsync(ids, ct);
        var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(ids, ct);

        var (allowedSet, hasUserContext) = await ResolveAccessibleServerIdsAsync(ct);
        if (!hasUserContext)
            return Unauthorized(ApiResponse<VpnServersV2Response>.ErrorResponse("User id missing from token."));

        foreach (var server in serversList)
        {
            var dto = server.Adapt<VpnServerDto>();
            dto.Tags = tagNamesByServer.GetValueOrDefault(server.Id, []);
            var v2 = dto.Adapt<VpnServerV2Dto>();
            v2.QuotaPlanGroups = groups.GetValueOrDefault(server.Id, []);
            v2.IsAccessibleForUserQuotaPlan = allowedSet is null || allowedSet.Contains(server.Id);
            response.VpnServers.Add(v2);
        }

        return Ok(ApiResponse<VpnServersV2Response>.SuccessResponse(response));
    }

    [HttpGet("get-all-with-status")]
    public async Task<ActionResult<ApiResponse<VpnServerWithStatusesV2Response>>> GetAllServersWithStatus(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var result = await openVpnServerOverviewQuery.GetAllVpnServersWithStatusAsync(
            includeDeleted, requireQuotaPlanAssignment: false, restrictToQuotaPlanId: null, ct);
        var response = new VpnServerWithStatusesV2Response();

        if (result.Count == 0)
            return Ok(ApiResponse<VpnServerWithStatusesV2Response>.SuccessResponse(response));

        var (allowedSet, hasUserContext) = await ResolveAccessibleServerIdsAsync(ct);
        if (!hasUserContext)
            return Unauthorized(ApiResponse<VpnServerWithStatusesV2Response>.ErrorResponse("User id missing from token."));

        var ids = result.Select(x => x.VpnServerResponses.VpnServer.Id).ToList();
        var groups = await quotaPlanGroupsQuery.GetGroupsByVpnServerIdsAsync(ids, ct);
        var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(ids, ct);

        foreach (var item in result)
        {
            var id = item.VpnServerResponses.VpnServer.Id;
            item.VpnServerResponses.VpnServer.Tags = tagNamesByServer.GetValueOrDefault(id, []);
            var v2 = new VpnServerWithStatusV2Dto
            {
                VpnServerResponses = new VpnServerV2Response
                {
                    VpnServer = item.VpnServerResponses.VpnServer.Adapt<VpnServerV2Dto>()
                },
                VpnServerStatusLogResponse = item.VpnServerStatusLogResponse,
                CountConnectedClients = item.CountConnectedClients,
                CountSessions = item.CountSessions,
                TotalBytesIn = item.TotalBytesIn,
                TotalBytesOut = item.TotalBytesOut
            };
            v2.VpnServerResponses.VpnServer.QuotaPlanGroups = groups.GetValueOrDefault(id, []);
            v2.VpnServerResponses.VpnServer.IsAccessibleForUserQuotaPlan =
                allowedSet is null || allowedSet.Contains(id);
            response.VpnServerWithStatuses.Add(v2);
        }

        return Ok(ApiResponse<VpnServerWithStatusesV2Response>.SuccessResponse(response));
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
            return (null, true);
        var set = await quotaPlanAllowedServerQueryService.GetVpnServerIdsByQuotaPlanId(uqp.QuotaPlanId, ct);
        return (set, true);
    }
}

