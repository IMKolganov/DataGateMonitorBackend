using DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Dto;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.LocalIssuance;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Controllers;

/// <summary>Origin / service-profile extras for <c>api/open-vpn-files/get-all/{vpnServerId}</c> rows.</summary>
[ApiController]
[Route("api/open-vpn-files")]
[Authorize]
[Authorize(Roles = "Admin,VpnUser,App")]
public class OpenVpnFileOriginsController(
    ApplicationDbContext db,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : ControllerBase
{
    [HttpGet("origins/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<List<IssuedOvpnFileOriginDto>>>> GetOrigins(
        [FromRoute] int vpnServerId, CancellationToken ct)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<List<IssuedOvpnFileOriginDto>>(User,
                vpnServerAccessQueryService, vpnServerId, ct) is { } deny)
            return deny;

        var rows = await db.IssuedOvpnFiles
                .Where(f => f.VpnServerId == vpnServerId)
                .Select(f => new IssuedOvpnFileOriginDto
                {
                    IssuedOvpnFileId = f.Id,
                    Origin = f.Origin == IssuedOvpnFileOrigin.Local ? "local" : "dashboard",
                    IsServiceProfile = f.IsServiceProfile,
                    CertSerialNumber = f.CertSerialNumber,
                    CertFingerprintSha256 = f.CertFingerprintSha256
                })
            .ToListAsync(ct);
        return Ok(ApiResponse<List<IssuedOvpnFileOriginDto>>.SuccessResponse(rows));
    }
}
