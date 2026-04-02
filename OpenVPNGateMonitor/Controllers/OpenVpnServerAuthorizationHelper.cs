using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

public static class OpenVpnServerAuthorizationHelper
{
    public static async Task<IActionResult?> RequireVpnServerAccessOrForbidAsync(
        ClaimsPrincipal user,
        IVpnServerAccessQueryService access,
        int vpnServerId,
        CancellationToken ct)
    {
        if (HttpUserContext.IsPrivileged(user))
            return null;
        if (!HttpUserContext.TryGetUserId(user, out var userId))
            return new UnauthorizedObjectResult(ApiResponse<string>.ErrorResponse("User id missing from token."));
        if (!await access.UserHasAccessAsync(userId, vpnServerId, ct))
            return new ObjectResult(ApiResponse<string>.ErrorResponse(
                    "Access to this VPN server is denied for your quota plan."))
                { StatusCode = StatusCodes.Status403Forbidden };
        return null;
    }
}
