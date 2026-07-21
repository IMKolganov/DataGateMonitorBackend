using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.TvLoginSessionTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.TvLogin;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.TvLogin;

public class TvLoginSessionServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_ReturnsSixDigitCodeAndHubPath()
    {
        var sessions = new List<TvLoginSession>();
        var sut = CreateSut(sessions, out _, out _, out _);

        var result = await sut.CreateSessionAsync(
            new CreateTvLoginSessionRequest { DeviceName = "Living Room TV", Client = "android-tv" },
            "127.0.0.1",
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Matches(@"^\d{6}$", result.UserCode);
        Assert.Equal("https://dash.datagateapp.com/tv/link", result.VerificationUrl);
        Assert.Equal(
            $"https://dash.datagateapp.com/tv/link?code={Uri.EscapeDataString(result.UserCode)}",
            result.QrPayload);
        Assert.Equal(2, result.PollIntervalSeconds);
        Assert.Equal(TvLoginHub.HubPath, result.SignalRHubPath);
        Assert.Single(sessions);
        Assert.Equal(TvLoginSessionStatus.Pending, sessions[0].Status);
        Assert.Equal(6, sessions[0].UserCode.Length);
    }

    [Fact]
    public async Task PollSessionAsync_WhenPending_ReturnsPendingWithoutTokens()
    {
        var sessionId = Guid.NewGuid();
        var sessions = new List<TvLoginSession>
        {
            new()
            {
                Id = sessionId,
                UserCode = "482913",
                Status = TvLoginSessionStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            }
        };
        var sut = CreateSut(sessions, out var tokenService, out _, out _);

        var poll = await sut.PollSessionAsync(sessionId, "1.1.1.1", CancellationToken.None);

        Assert.Equal("pending", poll.Status);
        Assert.Null(poll.Token);
        tokenService.Verify(
            t => t.IssueAsync(
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByUserCodeAsync_MarksViewed_AndNotifiesHub()
    {
        var sessionId = Guid.NewGuid();
        var sessions = new List<TvLoginSession>
        {
            new()
            {
                Id = sessionId,
                UserCode = "123456",
                Status = TvLoginSessionStatus.Pending,
                DeviceName = "Kitchen TV",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(4),
            }
        };
        var sut = CreateSut(sessions, out _, out _, out var hub);

        var preview = await sut.GetByUserCodeAsync("123 456", CancellationToken.None);

        Assert.Equal(sessionId, preview.SessionId);
        Assert.Equal("123456", preview.UserCode);
        Assert.Equal("pending", preview.Status);
        Assert.Equal(TvLoginSessionStatus.Viewed, sessions[0].Status);
        hub.Verify(
            h => h.NotifyStatusAsync(sessionId, "viewed", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var poll = await sut.PollSessionAsync(sessionId, "1.1.1.1", CancellationToken.None);
        Assert.Equal("viewed", poll.Status);
    }

    [Fact]
    public async Task ApproveThenPoll_DeliversTokensOnce_ThenConsumed()
    {
        var sessionId = Guid.NewGuid();
        var sessions = new List<TvLoginSession>
        {
            new()
            {
                Id = sessionId,
                UserCode = "654321",
                Status = TvLoginSessionStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            }
        };
        var sut = CreateSut(sessions, out var tokenService, out var userQuery, out var hub);

        userQuery.Setup(u => u.GetById(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                foreach (var s in sessions.Where(x =>
                             x.Status is TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed))
                    s.ApprovedUserId = id;
                return new User { Id = 42, DisplayName = "Alice", Email = "a@example.com" };
            });

        tokenService
            .Setup(t => t.IssueAsync(42, null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair(
                "access-token",
                DateTimeOffset.UtcNow.AddMinutes(15),
                "refresh-token",
                DateTimeOffset.UtcNow.AddDays(30)));

        var approve = await sut.ApproveAsync(
            new ApproveTvLoginSessionRequest { SessionId = sessionId },
            approvingUserId: 42,
            clientIp: "9.9.9.9",
            ct: CancellationToken.None);
        Assert.Equal("approved", approve.Status);
        hub.Verify(
            h => h.NotifyStatusAsync(sessionId, "approved", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var firstPoll = await sut.PollSessionAsync(sessionId, "2.2.2.2", CancellationToken.None);
        Assert.Equal("approved", firstPoll.Status);
        Assert.Equal("access-token", firstPoll.Token);
        Assert.Equal("refresh-token", firstPoll.RefreshToken);

        var secondPoll = await sut.PollSessionAsync(sessionId, "2.2.2.2", CancellationToken.None);
        Assert.Equal("consumed", secondPoll.Status);
        Assert.Null(secondPoll.Token);

        tokenService.Verify(
            t => t.IssueAsync(42, null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PollSessionAsync_WhenExpired_ReturnsExpired()
    {
        var sessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var sessions = new List<TvLoginSession>
        {
            new()
            {
                Id = sessionId,
                UserCode = "111222",
                Status = TvLoginSessionStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            }
        };
        var sut = CreateSut(sessions, out _, out _, out var hub);

        var poll = await sut.PollSessionAsync(sessionId, "3.3.3.3", CancellationToken.None);

        Assert.Equal("expired", poll.Status);
        Assert.Equal(TvLoginSessionStatus.Expired, sessions[0].Status);
        hub.Verify(
            h => h.NotifyStatusAsync(sessionId, "expired", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void NormalizeUserCode_KeepsDigitsOnly()
    {
        Assert.Equal("482913", TvLoginSessionService.NormalizeUserCode("482 913"));
        Assert.Equal("482913", TvLoginSessionService.NormalizeUserCode("482-913"));
        Assert.Equal("012345", TvLoginSessionService.FormatUserCode("012345"));
    }

    private static TvLoginSessionService CreateSut(
        List<TvLoginSession> sessions,
        out Mock<ITokenService> tokenService,
        out Mock<IUserQueryService> userQuery,
        out Mock<ITvLoginHubNotifier> hub)
    {
        var query = new Mock<ITvLoginSessionQueryService>();
        query.Setup(q => q.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => sessions.FirstOrDefault(s => s.Id == id));
        query.Setup(q => q.GetActiveByUserCode(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) =>
                sessions.FirstOrDefault(s =>
                    s.UserCode == code
                    && s.Status is TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed
                    && s.ExpiresAt > DateTimeOffset.UtcNow));
        query.Setup(q => q.GetLatestByUserCode(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) =>
                sessions.Where(s => s.UserCode == code).OrderByDescending(s => s.CreateDate).FirstOrDefault());
        query.Setup(q => q.AnyActiveByUserCode(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) =>
                sessions.Any(s =>
                    s.UserCode == code
                    && s.Status is TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed
                    && s.ExpiresAt > DateTimeOffset.UtcNow));

        var command = new Mock<ICommandService<TvLoginSession, Guid>>();
        command.Setup(c => c.Add(It.IsAny<TvLoginSession>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TvLoginSession e, bool _, CancellationToken _) =>
            {
                sessions.Add(e);
                return e;
            });

        command.Setup(c => c.UpdateWhere(
                It.IsAny<System.Linq.Expressions.Expression<Func<TvLoginSession, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<TvLoginSession>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                System.Linq.Expressions.Expression<Func<TvLoginSession, bool>> predicate,
                Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<TvLoginSession>> _,
                CancellationToken __) =>
            {
                var compiled = predicate.Compile();
                var matches = sessions.Where(compiled).ToList();
                foreach (var s in matches)
                {
                    if (s.Status is TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed
                        && s.ExpiresAt <= DateTimeOffset.UtcNow)
                    {
                        s.Status = TvLoginSessionStatus.Expired;
                    }
                    else if (s.Status == TvLoginSessionStatus.Approved)
                    {
                        s.Status = TvLoginSessionStatus.Consumed;
                    }
                    else if (s.Status is TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed
                             && s.ApprovedUserId is not null)
                    {
                        s.Status = TvLoginSessionStatus.Approved;
                        s.CompletedAt = DateTimeOffset.UtcNow;
                    }
                    else if (s.Status == TvLoginSessionStatus.Pending)
                    {
                        s.Status = TvLoginSessionStatus.Viewed;
                    }
                    else if (s.Status == TvLoginSessionStatus.Viewed)
                    {
                        s.Status = TvLoginSessionStatus.Denied;
                        s.CompletedAt = DateTimeOffset.UtcNow;
                    }
                }

                return matches.Count;
            });

        tokenService = new Mock<ITokenService>();
        userQuery = new Mock<IUserQueryService>();
        hub = new Mock<ITvLoginHubNotifier>();

        userQuery
            .Setup(u => u.GetById(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                foreach (var s in sessions.Where(x =>
                             x.Status is TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed))
                    s.ApprovedUserId = id;
                return new User { Id = id, DisplayName = "User" + id };
            });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:PublicWebBaseUrl"] = "https://dash.datagateapp.com",
                ["Auth:TvLoginSessionMinutes"] = "5",
            })
            .Build();

        var http = new Mock<IHttpContextAccessor>();
        http.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

        return new TvLoginSessionService(
            query.Object,
            command.Object,
            userQuery.Object,
            tokenService.Object,
            hub.Object,
            new MemoryCache(new MemoryCacheOptions()),
            config,
            http.Object,
            NullLogger<TvLoginSessionService>.Instance);
    }
}
