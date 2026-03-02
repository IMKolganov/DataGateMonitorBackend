using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerConflogTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-servers/conflog")]
[Authorize(Roles = "Admin,App")]
public class OpenVpnServerConflogController(
    IOpenVpnServerConflogService conflogService,
    IOpenVpnServerConflogQueryService conflogQueryService) : BaseController
{
    /// <summary>Request microservice INFO by URL; append to conflog only if payload changed. Returns new record or null when unchanged.</summary>
    [HttpPost("fetch-and-save")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerConflogResponse?>>> FetchAndSave(
        [FromBody] FetchAndSaveConflogRequest request,
        CancellationToken ct)
    {
        var entity = await conflogService.FetchAndSaveIfChangedAsync(
            request.BaseUrl, request.VpnServerId, ct);
        var response = entity == null
            ? null
            : new OpenVpnServerConflogResponse { Item = entity.Adapt<OpenVpnServerConflogDto>() };
        return Ok(ApiResponse<OpenVpnServerConflogResponse?>.SuccessResponse(response));
    }

    /// <summary>Request microservice INFO by server id (uses server ApiUrl); append to conflog only if payload changed.</summary>
    [HttpPost("fetch-and-save-by-server/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerConflogResponse?>>> FetchAndSaveByServer(
        int vpnServerId,
        CancellationToken ct)
    {
        var entity = await conflogService.FetchAndSaveIfChangedByServerIdAsync(vpnServerId, ct);
        var response = entity == null
            ? null
            : new OpenVpnServerConflogResponse { Item = entity.Adapt<OpenVpnServerConflogDto>() };
        return Ok(ApiResponse<OpenVpnServerConflogResponse?>.SuccessResponse(response));
    }

    /// <summary>Get one conflog record by id.</summary>
    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerConflogResponse>>> GetById(int id, CancellationToken ct)
    {
        var entity = await conflogQueryService.GetById(id, ct);
        if (entity == null)
            return NotFound(ApiResponse<OpenVpnServerConflogResponse>.ErrorResponse("Conflog record not found"));
        return Ok(ApiResponse<OpenVpnServerConflogResponse>.SuccessResponse(
            new OpenVpnServerConflogResponse { Item = entity.Adapt<OpenVpnServerConflogDto>() }));
    }

    /// <summary>Get conflog history by VPN server id (paged, newest first).</summary>
    [HttpGet("history-by-server/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerConflogPageResponse>>> GetHistoryByServer(
        int vpnServerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var paged = await conflogQueryService.GetPageByVpnServerId(vpnServerId, page, pageSize, ct);
        var response = new OpenVpnServerConflogPageResponse
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Adapt<List<OpenVpnServerConflogDto>>()
        };
        return Ok(ApiResponse<OpenVpnServerConflogPageResponse>.SuccessResponse(response));
    }

    /// <summary>Get all conflog records (paged, newest first).</summary>
    [HttpGet("get-page")]
    public async Task<ActionResult<ApiResponse<OpenVpnServerConflogPageResponse>>> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var paged = await conflogQueryService.GetPage(page, pageSize, ct);
        var response = new OpenVpnServerConflogPageResponse
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Adapt<List<OpenVpnServerConflogDto>>()
        };
        return Ok(ApiResponse<OpenVpnServerConflogPageResponse>.SuccessResponse(response));
    }
}
