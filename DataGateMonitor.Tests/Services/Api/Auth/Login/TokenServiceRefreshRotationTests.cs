using System.Linq.Expressions;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.SharedModels.Auth;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DataGateMonitor.Tests.Services.Api.Auth.Login;

/// <summary>
/// Refresh rotation, reuse detection, and concurrent claim (atomic UpdateWhere).
/// </summary>
public class TokenServiceRefreshRotationTests
{
    private const string JwtSecret = "VeryStrongTestSecretKey1234567890";
    private const string JwtPepper = "test-pepper-at-least-32-chars-long";

    private sealed class SutBundle
    {
        public required TokenService Sut { get; init; }
        public required Mock<IUserRefreshTokenQueryService> RefreshQuery { get; init; }
        public required Mock<ICommandService<UserRefreshToken, int>> RefreshCommand { get; init; }
        public required Mock<IAdminIdleSessionTracker> IdleTracker { get; init; }
    }

    private static SutBundle CreateBundle(User user, string roleName = SystemRoles.VpnUserName)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:RefreshPepper"] = JwtPepper,
                ["Jwt:LifetimeMinutes"] = "60",
                ["Jwt:RefreshLifetimeDays"] = "30",
                ["Jwt:Issuer"] = "OpenVPNGateBackend",
                ["Jwt:Audience"] = "OpenVPNGateFrontend",
            })
            .Build();

        var userQuery = new Mock<IUserQueryService>();
        userQuery.Setup(q => q.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var roleService = new Mock<IUserRoleService>();
        roleService
            .Setup(s => s.GetUserRoleNameAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleName);

        var refreshQuery = new Mock<IUserRefreshTokenQueryService>();
        var refreshCommand = new Mock<ICommandService<UserRefreshToken, int>>();
        var nextId = 200;
        refreshCommand
            .Setup(c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRefreshToken t, bool _, CancellationToken _) =>
            {
                t.Id = nextId++;
                return t;
            });
        refreshCommand
            .Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        refreshCommand
            .Setup(c => c.Delete(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var identityLinks = new Mock<IUserIdentityLinkQueryService>();
        identityLinks
            .Setup(q => q.GetListByUserId(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityLink>());

        var idleTracker = new Mock<IAdminIdleSessionTracker>();
        idleTracker.Setup(t => t.IsExpired(It.IsAny<int>())).Returns(false);
        var idleTimeout = new Mock<IAdminIdleTimeoutProvider>();
        idleTimeout.Setup(p => p.GetMinutes()).Returns(15);
        idleTimeout.Setup(p => p.GetMinutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(15);

        var sut = new TokenService(
            config,
            userQuery.Object,
            roleService.Object,
            refreshQuery.Object,
            refreshCommand.Object,
            identityLinks.Object,
            idleTracker.Object,
            idleTimeout.Object);

        return new SutBundle
        {
            Sut = sut,
            RefreshQuery = refreshQuery,
            RefreshCommand = refreshCommand,
            IdleTracker = idleTracker,
        };
    }

    private static UserRefreshToken ActiveToken(int userId, int id = 100) => new()
    {
        Id = id,
        UserId = userId,
        TokenHash = "ignored-hash",
        CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(20),
        RevokedAt = null,
        ReplacedByTokenId = null,
    };

    [Fact]
    public async Task RefreshAsync_HappyPath_ClaimsOldTokenAtomically()
    {
        var user = new User { Id = 7, DisplayName = "U", IsBlocked = false };
        var bundle = CreateBundle(user);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveToken(user.Id));

        var pair = await bundle.Sut.RefreshAsync("raw-refresh", null, "ua", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(pair.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(pair.RefreshToken));
        bundle.RefreshCommand.Verify(
            c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        bundle.RefreshCommand.Verify(
            c => c.Delete(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenReusingRotatedToken_RevokesFamilyAndThrows()
    {
        var user = new User { Id = 8, DisplayName = "U", IsBlocked = false };
        var bundle = CreateBundle(user);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRefreshToken
            {
                Id = 100,
                UserId = user.Id,
                TokenHash = "old-hash",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(20),
                // Outside default 60s grace → stolen-token family revoke.
                RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                ReplacedByTokenId = 201,
            });

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bundle.Sut.RefreshAsync("stolen-or-stale-refresh", null, "ua", CancellationToken.None));

        Assert.Contains("revoked", ex.Message, StringComparison.OrdinalIgnoreCase);
        bundle.RefreshCommand.Verify(
            c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        bundle.RefreshCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenLoserRetriesWithinGrace_DoesNotRevokeFamily()
    {
        var user = new User { Id = 11, DisplayName = "U", IsBlocked = false };
        var bundle = CreateBundle(user);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRefreshToken
            {
                Id = 100,
                UserId = user.Id,
                TokenHash = "old-hash",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(20),
                // Concurrent tab retry shortly after winner rotated.
                RevokedAt = DateTimeOffset.UtcNow.AddSeconds(-2),
                ReplacedByTokenId = 201,
            });

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bundle.Sut.RefreshAsync("stale-concurrent-refresh", null, "ua", CancellationToken.None));

        Assert.Contains("revoked", ex.Message, StringComparison.OrdinalIgnoreCase);
        bundle.RefreshCommand.Verify(
            c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        bundle.RefreshCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenLogoutRevokedWithoutReplacement_DoesNotRevokeFamily()
    {
        var user = new User { Id = 13, DisplayName = "U", IsBlocked = false };
        var bundle = CreateBundle(user);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRefreshToken
            {
                Id = 100,
                UserId = user.Id,
                TokenHash = "old-hash",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(20),
                RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                ReplacedByTokenId = null,
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bundle.Sut.RefreshAsync("logout-revoked", null, "ua", CancellationToken.None));

        bundle.RefreshCommand.Verify(
            c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenAtomicClaimLoses_DeletesOrphanAndThrows_WithoutFamilyRevoke()
    {
        var user = new User { Id = 9, DisplayName = "U", IsBlocked = false };
        var bundle = CreateBundle(user);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveToken(user.Id));

        // First UpdateWhere is the claim → 0 (lost race). Must not call a second family revoke.
        bundle.RefreshCommand
            .Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bundle.Sut.RefreshAsync("raw-refresh", null, "ua", CancellationToken.None));

        Assert.Contains("revoked", ex.Message, StringComparison.OrdinalIgnoreCase);
        bundle.RefreshCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
        bundle.RefreshCommand.Verify(
            c => c.Delete(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
        bundle.RefreshCommand.Verify(
            c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_WhenUserBlocked_ThrowsUnauthorized_WithoutRotating()
    {
        var user = new User { Id = 10, DisplayName = "Blocked", IsBlocked = true };
        var bundle = CreateBundle(user);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveToken(user.Id));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bundle.Sut.RefreshAsync("raw-refresh", null, "ua", CancellationToken.None));

        Assert.Contains("blocked", ex.Message, StringComparison.OrdinalIgnoreCase);
        bundle.RefreshCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
        bundle.RefreshCommand.Verify(
            c => c.UpdateWhere(
                It.IsAny<Expression<Func<UserRefreshToken, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<UserRefreshToken>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenAdminIdleExpired_ThrowsUnauthorized_WithoutRotating()
    {
        var user = new User { Id = 14, DisplayName = "Admin", IsBlocked = false };
        var bundle = CreateBundle(user, SystemRoles.AdminName);
        bundle.IdleTracker.Setup(t => t.IsExpired(user.Id)).Returns(true);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveToken(user.Id));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bundle.Sut.RefreshAsync("raw-refresh", null, "ua", CancellationToken.None));

        Assert.Contains("inactivity", ex.Message, StringComparison.OrdinalIgnoreCase);
        bundle.RefreshCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
        bundle.IdleTracker.Verify(t => t.Touch(user.Id), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenExpired_ThrowsUnauthorized()
    {
        var user = new User { Id = 12, DisplayName = "U", IsBlocked = false };
        var bundle = CreateBundle(user);
        bundle.RefreshQuery
            .Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRefreshToken
            {
                Id = 100,
                UserId = user.Id,
                TokenHash = "h",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-40),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                RevokedAt = null,
            });

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => bundle.Sut.RefreshAsync("raw-refresh", null, "ua", CancellationToken.None));

        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
        bundle.RefreshCommand.Verify(
            c => c.Add(It.IsAny<UserRefreshToken>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
