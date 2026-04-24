using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-servers/conflog")]
[Authorize(Roles = "Admin,App")]
public class VpnServerConflogController(
    IVpnServerConflogService conflogService,
    IVpnServerConflogQueryService conflogQueryService) : BaseController
{
    /// <summary>Request microservice INFO by URL; append to conflog only if payload changed. Returns new record or null when unchanged.</summary>
    [HttpPost("fetch-and-save")]
    public async Task<ActionResult<ApiResponse<VpnServerConflogResponse?>>> FetchAndSave(
        [FromBody] FetchAndSaveConflogRequest request,
        CancellationToken ct)
    {
        var entity = await conflogService.FetchAndSaveIfChangedAsync(
            request.BaseUrl, request.VpnServerId, ct);
        var response = entity == null
            ? null
            : new VpnServerConflogResponse { Item = entity.Adapt<VpnServerConflogDto>() };
        return Ok(ApiResponse<VpnServerConflogResponse?>.SuccessResponse(response));
    }

    /// <summary>Request microservice INFO by server id (uses server ApiUrl); append to conflog only if payload changed.</summary>
    [HttpPost("fetch-and-save-by-server/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<VpnServerConflogResponse?>>> FetchAndSaveByServer(
        int vpnServerId,
        CancellationToken ct)
    {
        var entity = await conflogService.FetchAndSaveIfChangedByServerIdAsync(vpnServerId, ct);
        var response = entity == null
            ? null
            : new VpnServerConflogResponse { Item = entity.Adapt<VpnServerConflogDto>() };
        return Ok(ApiResponse<VpnServerConflogResponse?>.SuccessResponse(response));
    }

    /// <summary>Get one conflog record by id.</summary>
    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<ApiResponse<VpnServerConflogResponse>>> GetById(int id, CancellationToken ct)
    {
        var entity = await conflogQueryService.GetById(id, ct);
        if (entity == null)
            return NotFound(ApiResponse<VpnServerConflogResponse>.ErrorResponse("Conflog record not found"));
        return Ok(ApiResponse<VpnServerConflogResponse>.SuccessResponse(
            new VpnServerConflogResponse { Item = entity.Adapt<VpnServerConflogDto>() }));
    }

    /// <summary>Get conflog history by VPN server id (paged, newest first).</summary>
    [HttpGet("history-by-server/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<VpnServerConflogPageResponse>>> GetHistoryByServer(
        int vpnServerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var paged = await conflogQueryService.GetPageByVpnServerId(vpnServerId, page, pageSize, ct);
        var response = new VpnServerConflogPageResponse
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Adapt<List<VpnServerConflogDto>>()
        };
        return Ok(ApiResponse<VpnServerConflogPageResponse>.SuccessResponse(response));
    }

    /// <summary>Get all conflog records (paged, newest first).</summary>
    [HttpGet("get-page")]
    public async Task<ActionResult<ApiResponse<VpnServerConflogPageResponse>>> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var paged = await conflogQueryService.GetPage(page, pageSize, ct);
        var response = new VpnServerConflogPageResponse
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Adapt<List<VpnServerConflogDto>>()
        };
        return Ok(ApiResponse<VpnServerConflogPageResponse>.SuccessResponse(response));
    }
}
