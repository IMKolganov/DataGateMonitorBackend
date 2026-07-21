using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.TvLogin;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.TvLogin;

public class TvLoginSessionServiceTests
{
    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSessionAsync_ReturnsSixDigitCode_QrPayload_AndHubPath()
    {
        var h = new TvLoginSessionServiceHarness();
        var sut = h.CreateSut();

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
        Assert.Single(h.Sessions);
        Assert.Equal(TvLoginSessionStatus.Pending, h.Sessions[0].Status);
        Assert.Equal("Living Room TV", h.Sessions[0].DeviceName);
        Assert.Equal("android-tv", h.Sessions[0].Client);
    }

    [Fact]
    public async Task CreateSessionAsync_UsesConfiguredTtl_And_TrimsTrailingSlashOnBaseUrl()
    {
        var h = new TvLoginSessionServiceHarness(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:PublicWebBaseUrl"] = "https://dash.datagateapp.com/",
                ["Auth:TvLoginSessionMinutes"] = "3",
            })
            .Build());
        var sut = h.CreateSut();
        var before = DateTimeOffset.UtcNow;

        var result = await sut.CreateSessionAsync(new CreateTvLoginSessionRequest(), "1.2.3.4", CancellationToken.None);

        Assert.Equal("https://dash.datagateapp.com/tv/link", result.VerificationUrl);
        Assert.InRange(result.ExpiresAt, before.AddMinutes(2.5), before.AddMinutes(3.5));
    }

    [Fact]
    public async Task CreateSessionAsync_WhenTtlConfigInvalid_FallsBackToFiveMinutes()
    {
        var h = new TvLoginSessionServiceHarness(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:PublicWebBaseUrl"] = "https://dash.datagateapp.com",
                ["Auth:TvLoginSessionMinutes"] = "0",
            })
            .Build());
        var sut = h.CreateSut();
        var before = DateTimeOffset.UtcNow;

        var result = await sut.CreateSessionAsync(new CreateTvLoginSessionRequest(), null, CancellationToken.None);

        Assert.InRange(result.ExpiresAt, before.AddMinutes(4.5), before.AddMinutes(5.5));
    }

    [Fact]
    public async Task CreateSessionAsync_CapturesDeviceIdAndUserAgentFromHeaders()
    {
        var h = new TvLoginSessionServiceHarness();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "AndroidTV/1.0";
        httpContext.Request.Headers["X-Device-Id"] = "tv-install-1";
        h.Http.SetupGet(x => x.HttpContext).Returns(httpContext);
        var sut = h.CreateSut();

        await sut.CreateSessionAsync(new CreateTvLoginSessionRequest(), "10.0.0.1", CancellationToken.None);

        Assert.Equal("tv-install-1", h.Sessions[0].DeviceId);
        Assert.Equal("AndroidTV/1.0", h.Sessions[0].UserAgent);
    }

    [Fact]
    public async Task CreateSessionAsync_TruncatesLongDeviceNameAndClient()
    {
        var h = new TvLoginSessionServiceHarness();
        var sut = h.CreateSut();
        var longName = new string('N', 200);
        var longClient = new string('C', 100);

        await sut.CreateSessionAsync(
            new CreateTvLoginSessionRequest { DeviceName = longName, Client = longClient },
            "1.1.1.1",
            CancellationToken.None);

        Assert.Equal(128, h.Sessions[0].DeviceName!.Length);
        Assert.Equal(64, h.Sessions[0].Client!.Length);
    }

    [Fact]
    public async Task CreateSessionAsync_WhenRateLimited_Throws()
    {
        var h = new TvLoginSessionServiceHarness();
        var sut = h.CreateSut();
        const string ip = "203.0.113.10";

        for (var i = 0; i < 10; i++)
            await sut.CreateSessionAsync(new CreateTvLoginSessionRequest(), ip, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateSessionAsync(new CreateTvLoginSessionRequest(), ip, CancellationToken.None));
        Assert.Equal(TvLoginSessionService.RateLimitMessage, ex.Message);
    }

    // ── Poll statuses ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(TvLoginSessionStatus.Pending, "pending")]
    [InlineData(TvLoginSessionStatus.Viewed, "viewed")]
    [InlineData(TvLoginSessionStatus.Denied, "denied")]
    [InlineData(TvLoginSessionStatus.Expired, "expired")]
    [InlineData(TvLoginSessionStatus.Consumed, "consumed")]
    public async Task PollSessionAsync_ReturnsStatusWithoutTokens(TvLoginSessionStatus status, string expected)
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(status: status);
        var sut = h.CreateSut();

        var poll = await sut.PollSessionAsync(session.Id, "1.1.1.1", CancellationToken.None);

        Assert.Equal(expected, poll.Status);
        Assert.Null(poll.Token);
        Assert.Null(poll.RefreshToken);
        Assert.False(poll.RequiresTotp);
        Assert.Null(poll.LoginChallengeId);
        Assert.False(poll.RequiresTotpSetup);
        h.TokenService.Verify(
            t => t.IssueAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PollSessionAsync_WhenMissing_ThrowsNotFound()
    {
        var sut = new TvLoginSessionServiceHarness().CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.PollSessionAsync(Guid.NewGuid(), "1.1.1.1", CancellationToken.None));

        Assert.Equal(TvLoginSessionService.SessionNotFoundMessage, ex.Message);
    }

    [Fact]
    public async Task PollSessionAsync_WhenPendingExpired_MarksExpired_AndNotifiesHub()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(
            status: TvLoginSessionStatus.Pending,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        var sut = h.CreateSut();

        var poll = await sut.PollSessionAsync(session.Id, "3.3.3.3", CancellationToken.None);

        Assert.Equal("expired", poll.Status);
        Assert.Equal(TvLoginSessionStatus.Expired, session.Status);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(session.Id, "expired", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PollSessionAsync_WhenViewedExpired_MarksExpired_AndNotifiesHub()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(
            status: TvLoginSessionStatus.Viewed,
            expiresAt: DateTimeOffset.UtcNow.AddSeconds(-5));
        var sut = h.CreateSut();

        var poll = await sut.PollSessionAsync(session.Id, "3.3.3.4", CancellationToken.None);

        Assert.Equal("expired", poll.Status);
        Assert.Equal(TvLoginSessionStatus.Expired, session.Status);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(session.Id, "expired", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Phone preview / viewed ──────────────────────────────────────────────

    [Fact]
    public async Task GetByUserCodeAsync_MarksViewed_AndNotifiesHub()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(userCode: "123456", deviceName: "Kitchen TV");
        var sut = h.CreateSut();

        var preview = await sut.GetByUserCodeAsync("123 456", CancellationToken.None);

        Assert.Equal(session.Id, preview.SessionId);
        Assert.Equal("123456", preview.UserCode);
        Assert.Equal("Kitchen TV", preview.DeviceName);
        Assert.Equal("pending", preview.Status); // phone UI still "pending"
        Assert.Equal(TvLoginSessionStatus.Viewed, session.Status);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(session.Id, "viewed", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var poll = await sut.PollSessionAsync(session.Id, "1.1.1.1", CancellationToken.None);
        Assert.Equal("viewed", poll.Status);
    }

    [Fact]
    public async Task GetByUserCodeAsync_WhenAlreadyViewed_DoesNotNotifyAgain()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(userCode: "222333", status: TvLoginSessionStatus.Viewed);
        var sut = h.CreateSut();

        var preview = await sut.GetByUserCodeAsync("222333", CancellationToken.None);

        Assert.Equal(session.Id, preview.SessionId);
        Assert.Equal("pending", preview.Status);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(It.IsAny<Guid>(), "viewed", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public async Task GetByUserCodeAsync_WhenCodeInvalidLength_ThrowsNotFound(string code)
    {
        var sut = new TvLoginSessionServiceHarness().CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetByUserCodeAsync(code, CancellationToken.None));

        Assert.Equal(TvLoginSessionService.SessionNotFoundMessage, ex.Message);
    }

    [Fact]
    public async Task GetByUserCodeAsync_WhenMissingEntirely_ThrowsNotFound()
    {
        var sut = new TvLoginSessionServiceHarness().CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetByUserCodeAsync("999888", CancellationToken.None));

        Assert.Equal(TvLoginSessionService.SessionNotFoundMessage, ex.Message);
    }

    [Fact]
    public async Task GetByUserCodeAsync_WhenOnlyExpiredExists_ThrowsExpired()
    {
        var h = new TvLoginSessionServiceHarness();
        h.AddSession(userCode: "555666", status: TvLoginSessionStatus.Expired);
        var sut = h.CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetByUserCodeAsync("555666", CancellationToken.None));

        Assert.Equal(TvLoginSessionService.SessionExpiredMessage, ex.Message);
    }

    // ── Approve / deny ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveThenPoll_DeliversTokensOnce_ThenConsumed()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(userCode: "654321");
        h.UserQuery.Setup(u => u.GetById(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                session.ApprovedUserId = id;
                return new User { Id = 42, DisplayName = "Alice", Email = "a@example.com" };
            });
        h.TokenService
            .Setup(t => t.IssueAsync(42, null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair(
                "access-token",
                DateTimeOffset.UtcNow.AddMinutes(15),
                "refresh-token",
                DateTimeOffset.UtcNow.AddDays(30)));
        var sut = h.CreateSut();

        var approve = await sut.ApproveAsync(
            new ApproveTvLoginSessionRequest { SessionId = session.Id },
            42,
            "9.9.9.9",
            CancellationToken.None);

        Assert.Equal("approved", approve.Status);
        Assert.Equal(TvLoginSessionStatus.Approved, session.Status);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(session.Id, "approved", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var first = await sut.PollSessionAsync(session.Id, "2.2.2.2", CancellationToken.None);
        Assert.Equal("approved", first.Status);
        Assert.Equal("access-token", first.Token);
        Assert.Equal("refresh-token", first.RefreshToken);
        Assert.Equal(42, first.UserId);
        Assert.Equal("Alice", first.DisplayName);
        Assert.Equal("a@example.com", first.Email);
        Assert.False(first.RequiresTotp);
        Assert.Null(first.LoginChallengeId);
        Assert.False(first.RequiresTotpSetup);

        var second = await sut.PollSessionAsync(session.Id, "2.2.2.2", CancellationToken.None);
        Assert.Equal("consumed", second.Status);
        Assert.Null(second.Token);
        h.TokenService.Verify(
            t => t.IssueAsync(42, null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(session.Id, "consumed", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_ByUserCode_WorksAfterViewed()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(userCode: "777888", status: TvLoginSessionStatus.Viewed);
        h.UserQuery.Setup(u => u.GetById(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                session.ApprovedUserId = id;
                return new User { Id = 7, DisplayName = "Bob" };
            });
        var sut = h.CreateSut();

        var result = await sut.ApproveAsync(
            new ApproveTvLoginSessionRequest { UserCode = "777-888" },
            7,
            "8.8.8.8",
            CancellationToken.None);

        Assert.Equal("approved", result.Status);
        Assert.Equal(TvLoginSessionStatus.Approved, session.Status);
    }

    [Fact]
    public async Task ApproveAsync_WhenUserBlocked_ThrowsUnauthorized()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession();
        h.UserQuery.Setup(u => u.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 5, DisplayName = "X", IsBlocked = true });
        var sut = h.CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.ApproveAsync(
                new ApproveTvLoginSessionRequest { SessionId = session.Id },
                5,
                "1.1.1.1",
                CancellationToken.None));
    }

    [Fact]
    public async Task ApproveAsync_WhenUserMissing_ThrowsUnauthorized()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession();
        h.UserQuery.Setup(u => u.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var sut = h.CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.ApproveAsync(
                new ApproveTvLoginSessionRequest { SessionId = session.Id },
                5,
                "1.1.1.1",
                CancellationToken.None));
    }

    [Fact]
    public async Task ApproveAsync_WhenAlreadyApproved_ThrowsNotPending()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(status: TvLoginSessionStatus.Approved, approvedUserId: 1);
        var sut = h.CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ApproveAsync(
                new ApproveTvLoginSessionRequest { SessionId = session.Id },
                1,
                "1.1.1.1",
                CancellationToken.None));

        Assert.Equal(TvLoginSessionService.SessionNotPendingMessage, ex.Message);
    }

    [Fact]
    public async Task DenyAsync_FromViewed_SetsDenied_AndNotifiesHub()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(status: TvLoginSessionStatus.Viewed);
        // Explicit deny transition (default heuristic also maps Viewed→Denied when ApprovedUserId is null).
        h.EnqueueUpdate(s =>
        {
            s.Status = TvLoginSessionStatus.Denied;
            s.CompletedAt = DateTimeOffset.UtcNow;
        });
        var sut = h.CreateSut();

        var result = await sut.DenyAsync(
            new DenyTvLoginSessionRequest { SessionId = session.Id },
            9,
            "4.4.4.4",
            CancellationToken.None);

        Assert.Equal("denied", result.Status);
        Assert.Equal(TvLoginSessionStatus.Denied, session.Status);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(session.Id, "denied", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var poll = await sut.PollSessionAsync(session.Id, "4.4.4.4", CancellationToken.None);
        Assert.Equal("denied", poll.Status);
    }

    [Fact]
    public async Task DenyAsync_FromPending_SetsDenied()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(status: TvLoginSessionStatus.Pending);
        h.EnqueueUpdate(s =>
        {
            s.Status = TvLoginSessionStatus.Denied;
            s.CompletedAt = DateTimeOffset.UtcNow;
        });
        var sut = h.CreateSut();

        var result = await sut.DenyAsync(
            new DenyTvLoginSessionRequest { UserCode = session.UserCode },
            3,
            "5.5.5.5",
            CancellationToken.None);

        Assert.Equal("denied", result.Status);
        Assert.Equal(TvLoginSessionStatus.Denied, session.Status);
    }

    [Fact]
    public async Task DenyAsync_WhenSessionMissing_ThrowsNotFound()
    {
        var sut = new TvLoginSessionServiceHarness().CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DenyAsync(
                new DenyTvLoginSessionRequest { SessionId = Guid.NewGuid() },
                1,
                "1.1.1.1",
                CancellationToken.None));

        Assert.Equal(TvLoginSessionService.SessionNotFoundMessage, ex.Message);
    }

    // ── Token delivery edge cases ───────────────────────────────────────────

    [Fact]
    public async Task PollApproved_WhenApprovedUserIdMissing_ReturnsExpiredWithoutTokens()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(status: TvLoginSessionStatus.Approved, approvedUserId: null);
        var sut = h.CreateSut();

        var poll = await sut.PollSessionAsync(session.Id, "1.1.1.1", CancellationToken.None);

        Assert.Equal("expired", poll.Status);
        Assert.Null(poll.Token);
        h.TokenService.Verify(
            t => t.IssueAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PollApproved_WhenUserBlocked_MarksConsumed_ReturnsExpired()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(status: TvLoginSessionStatus.Approved, approvedUserId: 11);
        h.UserQuery.Setup(u => u.GetById(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 11, DisplayName = "Blocked", IsBlocked = true });
        h.EnqueueUpdate(s => s.Status = TvLoginSessionStatus.Consumed);
        var sut = h.CreateSut();

        var poll = await sut.PollSessionAsync(session.Id, "1.1.1.1", CancellationToken.None);

        Assert.Equal("expired", poll.Status);
        Assert.Null(poll.Token);
        h.Hub.Verify(
            x => x.NotifyStatusAsync(session.Id, "consumed", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PollApproved_WhenClaimLosesRace_ReturnsConsumedWithoutIssuing()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(status: TvLoginSessionStatus.Approved, approvedUserId: 12);
        h.UserQuery.Setup(u => u.GetById(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 12, DisplayName = "Ok" });
        // Simulate lost race: UpdateWhere matches 0 rows.
        h.EnqueueUpdate(_ => { /* no-op; we'll force count via empty match by changing status first */ });
        // Better: change status before UpdateWhere by enqueue that sets Consumed then claim sees Approved→ already consumed
        // Actually UpdateWhere predicate requires Approved; if we set Consumed in enqueue, predicate won't match on second call.
        // Force claimed=0 by making the session no longer Approved before apply: change to Consumed in enqueue
        // but predicate runs first on Approved session...
        // Clear and use custom: set status to Consumed in enqueue (apply after match) — count still 1.
        // To get claimed != 1, enqueue nothing and make UpdateWhere return 0 by having session not match —
        // change session to Consumed before poll so DeliverTokensOnce isn't called... that won't hit the race path.

        // Re-setup: Approved session, but UpdateWhere returns 0 because we dequeue an apply that doesn't change
        // and we temporarily change status so second evaluation... The mock returns matches.Count after apply.
        // Trick: enqueue apply that sets Consumed, then... still count 1.

        // Use a second harness path: make UpdateWhere return 0 by clearing Approved before Apply —
        // Change mock via Enqueue that is empty and mutate Sessions in UserQuery callback:
        h.UserQuery.Setup(u => u.GetById(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, CancellationToken __) =>
            {
                session.Status = TvLoginSessionStatus.Consumed; // race: another poll consumed first
                return new User { Id = 12, DisplayName = "Ok" };
            });
        var sut = h.CreateSut();

        var poll = await sut.PollSessionAsync(session.Id, "1.1.1.1", CancellationToken.None);

        // After user load, status is Consumed so UpdateWhere matches 0 → "consumed"
        Assert.Equal("consumed", poll.Status);
        Assert.Null(poll.Token);
        h.TokenService.Verify(
            t => t.IssueAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PollApproved_UsesSessionDeviceMetadataForTokenIssue()
    {
        var h = new TvLoginSessionServiceHarness();
        var session = h.AddSession(
            status: TvLoginSessionStatus.Approved,
            approvedUserId: 15,
            deviceId: "dev-99",
            userAgent: "ua-99");
        h.UserQuery.Setup(u => u.GetById(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 15, DisplayName = "Dev" });
        h.TokenService
            .Setup(t => t.IssueAsync(15, null, "dev-99", "ua-99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair("a", DateTimeOffset.UtcNow.AddMinutes(1), "r", DateTimeOffset.UtcNow.AddDays(1)));
        var sut = h.CreateSut();

        var poll = await sut.PollSessionAsync(session.Id, "1.1.1.1", CancellationToken.None);

        Assert.Equal("approved", poll.Status);
        Assert.Equal("a", poll.Token);
        h.TokenService.Verify(
            t => t.IssueAsync(15, null, "dev-99", "ua-99", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Code helpers ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("482 913", "482913")]
    [InlineData("482-913", "482913")]
    [InlineData("48a2b913", "482913")]
    [InlineData("012345", "012345")]
    public void NormalizeUserCode_KeepsDigitsOnly(string? input, string expected)
    {
        Assert.Equal(expected, TvLoginSessionService.NormalizeUserCode(input));
    }

    [Fact]
    public void FormatUserCode_ReturnsNormalizedAsIs()
    {
        Assert.Equal("012345", TvLoginSessionService.FormatUserCode("012345"));
    }

    // ── Full lifecycle ──────────────────────────────────────────────────────

    [Fact]
    public async Task FullLifecycle_Create_View_Approve_PollTokens_PollConsumed()
    {
        var h = new TvLoginSessionServiceHarness();
        var sut = h.CreateSut();

        var created = await sut.CreateSessionAsync(
            new CreateTvLoginSessionRequest { DeviceName = "Den TV" },
            "10.0.0.2",
            CancellationToken.None);

        Assert.Equal("pending", (await sut.PollSessionAsync(created.SessionId, "10.0.0.2", CancellationToken.None)).Status);

        await sut.GetByUserCodeAsync(created.UserCode, CancellationToken.None);
        Assert.Equal("viewed", (await sut.PollSessionAsync(created.SessionId, "10.0.0.2", CancellationToken.None)).Status);

        var session = h.Sessions.Single(s => s.Id == created.SessionId);
        h.UserQuery.Setup(u => u.GetById(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                session.ApprovedUserId = id;
                return new User { Id = 100, DisplayName = "Owner", Email = "o@x.com" };
            });
        h.TokenService
            .Setup(t => t.IssueAsync(100, null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair("tok", DateTimeOffset.UtcNow.AddMinutes(10), "ref", DateTimeOffset.UtcNow.AddDays(7)));

        await sut.ApproveAsync(
            new ApproveTvLoginSessionRequest { SessionId = created.SessionId },
            100,
            "10.0.0.3",
            CancellationToken.None);

        var withTokens = await sut.PollSessionAsync(created.SessionId, "10.0.0.2", CancellationToken.None);
        Assert.Equal("approved", withTokens.Status);
        Assert.Equal("tok", withTokens.Token);

        var again = await sut.PollSessionAsync(created.SessionId, "10.0.0.2", CancellationToken.None);
        Assert.Equal("consumed", again.Status);
        Assert.Null(again.Token);
    }
}
