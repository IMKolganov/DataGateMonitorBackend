using System.Net.WebSockets;
using System.Text;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServersController(ILogger<OpenVpnServersController> logger, IVpnDataService vpnDataService,
    IOpenVpnServerOverviewQuery openVpnServerOverviewQuery,
    IOpenVpnServerQueryService openVpnServerQueryService,
    IOpenVpnBackgroundService openVpnBackgroundService) : ControllerBase
{
    [HttpGet("GetAllServersWithStatus")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerWithStatusResponse>>> GetAllServersWithStatus(CancellationToken cancellationToken)
    {
        var result = await openVpnServerOverviewQuery.GetAllOpenVpnServersWithStatusAsync(cancellationToken);
        return Ok(ApiResponse<List<OpenVpnServerWithStatusResponse>>.SuccessResponse(result.Adapt<List<OpenVpnServerWithStatusResponse>>()));
    }
    
    [HttpGet("GetServerWithStatus/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerWithStatusResponse>>> GetServerWithStatus(
        [FromRoute] GetServerWithStatsRequest request, CancellationToken cancellationToken)
    {
        var serverInfo = await openVpnServerOverviewQuery.GetOpenVpnServerWithStatusAsync(request.VpnServerId, cancellationToken);

        return Ok(ApiResponse<OpenVpnServerWithStatusResponse>.SuccessResponse(serverInfo.Adapt<OpenVpnServerWithStatusResponse>()));
    }

    [HttpGet("GetAllServers")]
    public async Task<ActionResult<ApiResponse<List<OpenVpnServerResponse>>>> GetAllServers(
        CancellationToken cancellationToken)
    {
        var serversList = await openVpnServerQueryService.GetAllAsync(cancellationToken);

        return Ok(ApiResponse<List<OpenVpnServerResponse>>.SuccessResponse(
            serversList.Adapt<List<OpenVpnServerResponse>>()));
    }
    
    [HttpGet("GetServer/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> GetServer(
        [FromRoute] GetServerRequest request, CancellationToken cancellationToken)
    {
        var server = await openVpnServerQueryService.GetByIdAsync(request.VpnServerId, cancellationToken);

        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(server.Adapt<OpenVpnServerResponse>()));
    }

    [HttpPost("AddServer")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> AddServer(
        [FromBody] AddServerRequest request, CancellationToken cancellationToken)
    {
        var newServer = await vpnDataService.AddOpenVpnServer(request.Adapt<OpenVpnServer>(), cancellationToken);

        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(newServer.Adapt<OpenVpnServerResponse>()));
    }
    
    [HttpPut("UpdateServer")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerResponse>>> UpdateServer(
        [FromBody] UpdateServerRequest request, CancellationToken cancellationToken)
    {
        var updatedServer = await vpnDataService.UpdateOpenVpnServer(request.Adapt<OpenVpnServer>(), cancellationToken);

        return Ok(ApiResponse<OpenVpnServerResponse>.SuccessResponse(updatedServer.Adapt<OpenVpnServerResponse>()));
    }

    [HttpDelete("DeleteServer/{vpnServerId:int}")]
    //todo: fixed response
    public async Task<IActionResult> DeleteServer(
        [FromRoute] DeleteServerRequest request, CancellationToken cancellationToken)
    {
        var deletedServer = await vpnDataService.DeleteOpenVpnServer(request.VpnServerId, cancellationToken);

        return Ok(ApiResponse<bool>.SuccessResponse(deletedServer));
    }
    
    [HttpGet("status")]
    public ActionResult<ApiResponse<List<ServiceStatusResponse>>> GetStatus()
    {
        var serverStatuses = openVpnBackgroundService.GetStatus()
            .Select(x => x.Adapt<ServiceStatusResponse>())
            .ToList();

        return Ok(ApiResponse<List<ServiceStatusResponse>>.SuccessResponse(serverStatuses));
    }

    [HttpPost("run-now")]
    public async Task<ActionResult<ApiResponse<string>>> RunNow(CancellationToken cancellationToken)
    {
        var serverStatuses = openVpnBackgroundService.GetStatus();

        if (serverStatuses.Values.All(x => x.Status != ServiceStatus.Running))
        {
            await openVpnBackgroundService.RunNow(cancellationToken);
        }

        return Ok(ApiResponse<string>.SuccessResponse("OpenVPN background task executed immediately."));
    }

    [HttpGet("status-stream")]
    public async Task StatusStream(CancellationToken cancellationToken)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await SendStatusUpdates(webSocket, cancellationToken);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    private async Task SendStatusUpdates(WebSocket webSocket, CancellationToken ct)
    {
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var statuses = openVpnBackgroundService.GetStatus().Values
                    .Select(x => x.Adapt<ServiceStatusResponse>())
                    .ToList();
                
                foreach (var status in statuses)
                {
                    var (connectedClients, sessions) =
                        await openVpnServerOverviewQuery.GetClientCountersAsync(status.VpnServerId, ct);

                    status.CountConnectedClients = connectedClients;
                    status.CountSessions = sessions;
                }

                var json = JsonConvert.SerializeObject(statuses);

                await webSocket.SendAsync(
                    Encoding.UTF8.GetBytes(json),
                    WebSocketMessageType.Text,
                    true,
                    ct);

                await Task.Delay(1000, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"WebSocket error: {ex.Message}");
        }
        finally
        {
            try
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error closing WebSocket: {ex.Message}");
            }
            logger.LogInformation("WebSocket closed.");
        }
    }
}
