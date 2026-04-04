using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

public static class OpenVpnServerAuthorizationHelper
{
    /// <typeparam name="T">Success payload type for <see cref="ApiResponse{T}"/> on this action (error bodies may still use <see cref="string"/>).</typeparam>
    public static async Task<ActionResult<ApiResponse<T>>?> RequireVpnServerAccessOrForbidAsync<T>(
        ClaimsPrincipal user,
        IVpnServerAccessQueryService access,
        int vpnServerId,
        CancellationToken ct)
    {
        if (HttpUserContext.IsPrivileged(user))
            return null;
        if (!HttpUserContext.TryGetUserId(user, out var userId))
            return new ActionResult<ApiResponse<T>>(new UnauthorizedObjectResult(
                ApiResponse<string>.ErrorResponse("User id missing from token.")));
        if (!await access.UserHasAccessAsync(userId, vpnServerId, ct))
            return new ActionResult<ApiResponse<T>>(new ObjectResult(ApiResponse<string>.ErrorResponse(
                    "Access to this VPN server is denied for your quota plan."))
                { StatusCode = StatusCodes.Status403Forbidden });
        return null;
    }
}
