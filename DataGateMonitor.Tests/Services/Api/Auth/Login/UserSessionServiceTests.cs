using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;

namespace DataGateMonitor.Tests.Services.Api.Auth.Login;

public class UserSessionServiceTests
{
    private readonly Mock<IUserRefreshTokenQueryService> _query = new(MockBehavior.Strict);
    private readonly Mock<ICommandService<UserRefreshToken, int>> _command = new(MockBehavior.Strict);
    private readonly UserSessionService _sut;

    public UserSessionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:RefreshPepper"] = "test-pepper-secret" })
            .Build();
        _sut = new UserSessionService(_query.Object, _command.Object, config);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_MarksCurrentSession()
    {
        var now = DateTimeOffset.UtcNow;
        const string rawToken = "refresh-token-raw";
        var hash = Hash(rawToken);

        _query.Setup(q => q.Search(It.IsAny<System.Linq.Expressions.Expression<Func<UserRefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new UserRefreshToken
                {
                    Id = 1,
                    UserId = 5,
                    DeviceId = "device-10",
                    TokenHash = hash,
                    UserAgent = "agent",
                    CreatedAt = now,
                    ExpiresAt = now.AddDays(7),
                },
                new UserRefreshToken
                {
                    Id = 2,
                    UserId = 5,
                    TokenHash = "other",
                    CreatedAt = now,
                    ExpiresAt = now.AddDays(7),
                },
            ]);

        var result = await _sut.GetActiveSessionsAsync(5, rawToken, CancellationToken.None);

        Assert.Equal(2, result.Sessions.Count);
        Assert.True(result.Sessions.Single(s => s.Id == 1).IsCurrent);
        Assert.False(result.Sessions.Single(s => s.Id == 2).IsCurrent);
    }

    [Fact]
    public async Task RevokeSessionAsync_RevokesMatchingSession()
    {
        var now = DateTimeOffset.UtcNow;
        var token = new UserRefreshToken { Id = 3, UserId = 5, TokenHash = "h", CreatedAt = now, ExpiresAt = now.AddDays(1) };
        _query.Setup(q => q.GetById(3, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _command.Setup(c => c.Update(It.Is<UserRefreshToken>(t => t.RevokedAt != null), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _sut.RevokeSessionAsync(5, 3, CancellationToken.None);

        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenWrongUser_ThrowsUnauthorized()
    {
        var token = new UserRefreshToken { Id = 3, UserId = 99, TokenHash = "h" };
        _query.Setup(q => q.GetById(3, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.RevokeSessionAsync(5, 3, CancellationToken.None));
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenAlreadyRevoked_IsNoOp()
    {
        var token = new UserRefreshToken { Id = 3, UserId = 5, TokenHash = "h", RevokedAt = DateTimeOffset.UtcNow };
        _query.Setup(q => q.GetById(3, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await _sut.RevokeSessionAsync(5, 3, CancellationToken.None);

        _command.Verify(c => c.Update(It.IsAny<UserRefreshToken>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeSessionsAsync_RevokesAllExceptKeptToken()
    {
        var now = DateTimeOffset.UtcNow;
        const string keepRaw = "keep-me";
        var keepHash = Hash(keepRaw);
        var keep = new UserRefreshToken { Id = 1, UserId = 5, TokenHash = keepHash, CreatedAt = now, ExpiresAt = now.AddDays(1) };
        var revoke = new UserRefreshToken { Id = 2, UserId = 5, TokenHash = "drop", CreatedAt = now, ExpiresAt = now.AddDays(1) };

        _query.Setup(q => q.Search(It.IsAny<System.Linq.Expressions.Expression<Func<UserRefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([keep, revoke]);
        _command.Setup(c => c.Update(It.Is<UserRefreshToken>(t => t.Id == 2), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var revoked = await _sut.RevokeSessionsAsync(5, keepRaw, CancellationToken.None);

        Assert.Equal(1, revoked);
        Assert.Null(keep.RevokedAt);
        Assert.NotNull(revoke.RevokedAt);
    }

    [Fact]
    public async Task RevokeByRefreshTokenAsync_WhenTokenMissing_IsNoOp()
    {
        _query.Setup(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRefreshToken?)null);

        await _sut.RevokeByRefreshTokenAsync("missing", CancellationToken.None);

        _command.Verify(c => c.Update(It.IsAny<UserRefreshToken>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeByRefreshTokenAsync_WhenBlankToken_IsNoOp()
    {
        await _sut.RevokeByRefreshTokenAsync("  ", CancellationToken.None);

        _query.Verify(q => q.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private string Hash(string rawToken)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:RefreshPepper"] = "test-pepper-secret" })
            .Build();
        var svc = new UserSessionService(_query.Object, _command.Object, config);
        return typeof(UserSessionService)
            .GetMethod("HashRefreshToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(svc, [rawToken])!
            .ToString()!;
    }
}
