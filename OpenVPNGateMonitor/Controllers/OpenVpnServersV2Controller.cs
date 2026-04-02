using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

/// <summary>
/// API v2: same server lists as v1, plus <see cref="OpenVpnServerV2Dto.QuotaPlanGroups"/> for UI grouping.
/// Lists only include servers linked to at least one quota plan via <c>QuotaPlanAllowedServers</c>.
/// </summary>
[ApiController]
[Route("api/v2/open-vpn-servers")]
[Authorize]
public class OpenVpnServersV2Controller(
    IOpenVpnServerOverviewQuery openVpnServerOverviewQuery,
    IOpenVpnServerQueryService openVpnServerQueryService,
    IOpenVpnServerQuotaPlanGroupsQuery quotaPlanGroupsQuery,
    IOpenVpnServerTagQueryService openVpnServerTagQueryService) : BaseController
{
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<OpenVpnServersV2Response>>> GetAllServers(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var serversList = await openVpnServerQueryService.GetAll(includeDeleted, requireQuotaPlanAssignment: true, ct);
        var response = new OpenVpnServersV2Response();
        if (serversList.Count == 0)
            return Ok(ApiResponse<OpenVpnServersV2Response>.SuccessResponse(response));

        var ids = serversList.Select(s => s.Id).ToList();
        var groups = await quotaPlanGroupsQuery.GetGroupsByVpnServerIdsAsync(ids, ct);
        var tagNamesByServer = await openVpnServerTagQueryService.GetTagNamesByVpnServerIds(ids, ct);

        foreach (var server in serversList)
        {
            var dto = server.Adapt<OpenVpnServerDto>();
            dto.Tags = tagNamesByServer.GetValueOrDefault(server.Id, []);
            var v2 = dto.Adapt<OpenVpnServerV2Dto>();
            v2.QuotaPlanGroups = groups.GetValueOrDefault(server.Id, []);
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
            includeDeleted, requireQuotaPlanAssignment: true, ct);
        var response = new OpenVpnServerWithStatusesV2Response();

        if (result.Count == 0)
            return Ok(ApiResponse<OpenVpnServerWithStatusesV2Response>.SuccessResponse(response));

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
            response.OpenVpnServerWithStatuses.Add(v2);
        }

        return Ok(ApiResponse<OpenVpnServerWithStatusesV2Response>.SuccessResponse(response));
    }
}
