using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnServerAuthorizationHelperTests
{
    [Fact]
    public async Task RequireVpnServerAccessOrForbidAsync_WhenAdmin_ReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "mock"));
        var access = new Mock<IVpnServerAccessQueryService>(MockBehavior.Strict);

        var result = await OpenVpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync(
            user, access.Object, 1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RequireVpnServerAccessOrForbidAsync_WhenVpnUserMissingUserId_ReturnsUnauthorized()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "VpnUser")], "mock"));
        var access = new Mock<IVpnServerAccessQueryService>(MockBehavior.Strict);

        var result = await OpenVpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync(
            user, access.Object, 1, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var api = Assert.IsType<ApiResponse<string>>(unauthorized.Value);
        Assert.False(api.Success);
    }

    [Fact]
    public async Task RequireVpnServerAccessOrForbidAsync_WhenNoAccess_ReturnsForbidden()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "42")
            ],
            "mock"));
        var access = new Mock<IVpnServerAccessQueryService>();
        access.Setup(a => a.UserHasAccessAsync(42, 7, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await OpenVpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync(
            user, access.Object, 7, CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
    }
}
