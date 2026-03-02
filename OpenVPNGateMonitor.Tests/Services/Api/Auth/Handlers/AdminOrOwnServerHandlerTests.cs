using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Api.Auth.Handlers;

public class AdminOrOwnServerHandlerTests
{
    [Fact]
    public async Task HandleAsync_When_UserIsAdmin_Succeeds()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            new[] { new AdminOrOwnServerRequirement() },
            user,
            10);
        var access = new Mock<IVpnServerAccessQueryService>();

        var sut = new AdminOrOwnServerHandler(access.Object);

        await sut.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        access.Verify(a => a.UserHasAccessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_When_NotAuthenticated_DoesNotSucceed()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            new[] { new AdminOrOwnServerRequirement() },
            user,
            10);
        var access = new Mock<IVpnServerAccessQueryService>();

        var sut = new AdminOrOwnServerHandler(access.Object);

        await sut.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        access.Verify(a => a.UserHasAccessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_When_UserHasAccess_Succeeds()
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "5") };
        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            new[] { new AdminOrOwnServerRequirement() },
            user,
            10);
        var access = new Mock<IVpnServerAccessQueryService>();
        access.Setup(a => a.UserHasAccessAsync(5, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = new AdminOrOwnServerHandler(access.Object);

        await sut.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        access.Verify(a => a.UserHasAccessAsync(5, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_When_UserHasNoAccess_DoesNotSucceed()
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "5") };
        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            new[] { new AdminOrOwnServerRequirement() },
            user,
            10);
        var access = new Mock<IVpnServerAccessQueryService>();
        access.Setup(a => a.UserHasAccessAsync(5, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = new AdminOrOwnServerHandler(access.Object);

        await sut.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
