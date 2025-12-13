using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth.Handlers;

public sealed class AdminOrOwnServerHandler(IVpnServerAccessQueryService access)
    : AuthorizationHandler<AdminOrOwnServerRequirement, int>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOrOwnServerRequirement requirement,
        int vpnServerId)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return;

        if (await access.UserHasAccessAsync(userId, vpnServerId, CancellationToken.None))
            context.Succeed(requirement);
    }
}