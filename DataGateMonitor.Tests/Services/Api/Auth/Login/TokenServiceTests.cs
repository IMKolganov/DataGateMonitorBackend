using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Login;

public class TokenServiceTests
{
    [Fact]
    public async Task IssueAsync_When_UserNotFound_ThrowsInvalidOperationException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection("Jwt")["RefreshLifetimeDays"]).Returns((string?)null);
        var userQuery = new Mock<IUserQueryService>();
        userQuery.Setup(q => q.GetById(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var userRoleService = new Mock<IUserRoleService>();
        var refreshTokenQuery = new Mock<IUserRefreshTokenQueryService>();
        var refreshTokenCommand = new Mock<ICommandService<UserRefreshToken, int>>();
        var userIdentityLinkQuery = new Mock<IUserIdentityLinkQueryService>();
        var adminIdleTracker = new Mock<IAdminIdleSessionTracker>();
        var adminIdleTimeout = new Mock<IAdminIdleTimeoutProvider>();
        adminIdleTimeout.Setup(p => p.GetMinutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(15);

        var sut = new TokenService(
            config.Object,
            userQuery.Object,
            userRoleService.Object,
            refreshTokenQuery.Object,
            refreshTokenCommand.Object,
            userIdentityLinkQuery.Object,
            adminIdleTracker.Object,
            adminIdleTimeout.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.IssueAsync(999, null, null, null, CancellationToken.None));

        Assert.Equal("User not found.", ex.Message);
        refreshTokenCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueAsync_When_UserBlocked_ThrowsUnauthorizedAccessException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection("Jwt")["RefreshLifetimeDays"]).Returns((string?)null);
        var user = new User { Id = 1, DisplayName = "U", IsBlocked = true };
        var userQuery = new Mock<IUserQueryService>();
        userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var userRoleService = new Mock<IUserRoleService>();
        var refreshTokenQuery = new Mock<IUserRefreshTokenQueryService>();
        var refreshTokenCommand = new Mock<ICommandService<UserRefreshToken, int>>();
        var userIdentityLinkQuery = new Mock<IUserIdentityLinkQueryService>();
        var adminIdleTracker = new Mock<IAdminIdleSessionTracker>();
        var adminIdleTimeout = new Mock<IAdminIdleTimeoutProvider>();
        adminIdleTimeout.Setup(p => p.GetMinutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(15);

        var sut = new TokenService(
            config.Object,
            userQuery.Object,
            userRoleService.Object,
            refreshTokenQuery.Object,
            refreshTokenCommand.Object,
            userIdentityLinkQuery.Object,
            adminIdleTracker.Object,
            adminIdleTimeout.Object);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.IssueAsync(1, null, null, null, CancellationToken.None));

        Assert.Equal("User account is blocked.", ex.Message);
        refreshTokenCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueAsync_When_DeviceIdProvided_RevokesPriorSessionsForDevice()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-at-least-32-bytes-long!!",
                ["Jwt:RefreshPepper"] = "test-pepper-secret",
                ["Jwt:RefreshLifetimeDays"] = "30",
                ["Jwt:LifetimeMinutes"] = "15",
            })
            .Build();

        var user = new User { Id = 7, DisplayName = "Admin", IsBlocked = false };
        var userQuery = new Mock<IUserQueryService>();
        userQuery.Setup(q => q.GetById(7, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var userRoleService = new Mock<IUserRoleService>();
        userRoleService.Setup(r => r.GetUserRoleNameAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Admin");

        var refreshTokenQuery = new Mock<IUserRefreshTokenQueryService>();
        var refreshTokenCommand = new Mock<ICommandService<UserRefreshToken, int>>();
        refreshTokenCommand
            .Setup(c => c.UpdateWhere(
                It.IsAny<System.Linq.Expressions.Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        refreshTokenCommand
            .Setup(c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRefreshToken t, bool _, CancellationToken _) => t);

        var userIdentityLinkQuery = new Mock<IUserIdentityLinkQueryService>();
        userIdentityLinkQuery
            .Setup(q => q.GetListByUserId(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var adminIdleTracker = new Mock<IAdminIdleSessionTracker>();
        var adminIdleTimeout = new Mock<IAdminIdleTimeoutProvider>();
        adminIdleTimeout.Setup(p => p.GetMinutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(15);

        var sut = new TokenService(
            config,
            userQuery.Object,
            userRoleService.Object,
            refreshTokenQuery.Object,
            refreshTokenCommand.Object,
            userIdentityLinkQuery.Object,
            adminIdleTracker.Object,
            adminIdleTimeout.Object);

        await sut.IssueAsync(7, null, "browser-1", "Chrome", CancellationToken.None);

        refreshTokenCommand.Verify(
            c => c.UpdateWhere(
                It.IsAny<System.Linq.Expressions.Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        refreshTokenCommand.Verify(
            c => c.Add(
                It.Is<UserRefreshToken>(t => t.UserId == 7 && t.DeviceId == "browser-1"),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
