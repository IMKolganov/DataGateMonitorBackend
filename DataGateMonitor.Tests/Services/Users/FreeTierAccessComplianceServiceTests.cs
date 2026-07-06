using Microsoft.Extensions.Caching.Memory;
using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.Services.TelegramBot;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.Services.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;

namespace DataGateMonitor.Tests.Services.Users;

public class FreeTierAccessComplianceServiceTests
{
    private readonly Mock<IUserQuotaPlanQueryService> _quotaAssignmentQuery = new();
    private readonly Mock<IQuotaPlanQueryService> _quotaPlanQuery = new();
    private readonly Mock<IUserIdentityLinkQueryService> _identityLinkQuery = new();
    private readonly Mock<ITelegramChannelMembershipChecker> _channelChecker = new();
    private readonly Mock<IAppNotificationFacade> _notifications = new();
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

    private FreeTierAccessComplianceService CreateSut()
        => new(
            _quotaAssignmentQuery.Object,
            _quotaPlanQuery.Object,
            _identityLinkQuery.Object,
            _channelChecker.Object,
            _notifications.Object,
            _settingsService.Object,
            _memoryCache,
            Options.Create(new TelegramChannelSettings { RequiredChannelUsername = "DataGateVPNBot" }),
            Mock.Of<ILogger<FreeTierAccessComplianceService>>());

    private void SetupGraceSettings(bool enabled, int minutes = 5)
    {
        _settingsService
            .Setup(s => s.GetValueAsync<string>(
                $"{FreeTierAccessSettingsKeys.AllowGraceWithoutCompliance}_Type",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("bool");
        _settingsService
            .Setup(s => s.GetValueAsync<bool>(
                FreeTierAccessSettingsKeys.AllowGraceWithoutCompliance,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(enabled);

        _settingsService
            .Setup(s => s.GetValueAsync<string>(
                $"{FreeTierAccessSettingsKeys.GracePeriodMinutes}_Type",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("int");
        _settingsService
            .Setup(s => s.GetValueAsync<int>(
                FreeTierAccessSettingsKeys.GracePeriodMinutes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(minutes);
    }

    private void SetupFreePlanUser(int userId, long telegramId = 888)
    {
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = userId, QuotaPlanId = 1 });
        _quotaPlanQuery
            .Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 1, Name = QuotaPlanNames.Free });
        _identityLinkQuery
            .Setup(q => q.GetListByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = telegramId.ToString() }]);
        _channelChecker
            .Setup(c => c.IsSubscribedAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task SkipsAudit_WhenUserHasNoActivePlan()
    {
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserQuotaPlan?)null);

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededAsync(1, "test", ct: CancellationToken.None);

        Assert.False(result.IsApplicable);
        Assert.True(result.IsCompliant);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SkipsAudit_WhenActivePlanIsNotFreeOrDefault()
    {
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 1, QuotaPlanId = 3 });
        _quotaPlanQuery
            .Setup(q => q.GetById(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 3, Name = "Pro" });

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededAsync(1, "test", ct: CancellationToken.None);

        Assert.False(result.IsApplicable);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsCompliant_WhenMergedTelegramGoogleAccountOnDefaultPlan()
    {
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 10, QuotaPlanId = 2 });
        _quotaPlanQuery
            .Setup(q => q.GetById(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 2, Name = QuotaPlanNames.Default });
        _identityLinkQuery
            .Setup(q => q.GetListByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserIdentityLink { Provider = "telegram", ExternalId = "12345" },
                new UserIdentityLink { Provider = "google", ExternalId = "google-sub" },
            ]);

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededAsync(10, "merge", ct: CancellationToken.None);

        Assert.True(result.IsApplicable);
        Assert.True(result.IsCompliant);
        Assert.True(result.IsMergedAccount);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsCompliant_WhenSubscribedToChannelOnFreePlan()
    {
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 11, QuotaPlanId = 1 });
        _quotaPlanQuery
            .Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 1, Name = QuotaPlanNames.Free });
        _identityLinkQuery
            .Setup(q => q.GetListByUserId(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "777" }]);

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededAsync(
            11,
            "bot",
            isChannelSubscribed: true,
            ct: CancellationToken.None);

        Assert.True(result.IsApplicable);
        Assert.True(result.IsCompliant);
        Assert.True(result.IsChannelSubscribed);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NotifiesAdmins_WhenFreePlanWithoutMergeOrChannel()
    {
        SetupFreePlanUser(12, 888);
        SetupGraceSettings(enabled: false);

        _notifications
            .Setup(n => n.FreeTierAccessNonCompliant(
                12,
                QuotaPlanNames.Free,
                888,
                false,
                false,
                "audit",
                "@DataGateVPNBot",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededAsync(12, "audit", ct: CancellationToken.None);

        Assert.True(result.IsApplicable);
        Assert.False(result.IsCompliant);
        Assert.True(result.AdminsNotified);
        _notifications.VerifyAll();
    }

    [Fact]
    public async Task IsCompliantViaGrace_WhenSettingEnabledAndNoMergeOrChannel()
    {
        SetupFreePlanUser(13, 999);
        SetupGraceSettings(enabled: true, minutes: 5);

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededAsync(13, "bot", ct: CancellationToken.None);

        Assert.True(result.IsApplicable);
        Assert.True(result.IsCompliant);
        Assert.True(result.IsGracePeriod);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GracePeriod_ReusesCacheEntry_WithoutRestartingWindow()
    {
        SetupFreePlanUser(14, 1000);
        SetupGraceSettings(enabled: true, minutes: 5);

        var sut = CreateSut();
        var first = await sut.AuditAndNotifyIfNeededAsync(14, "bot", ct: CancellationToken.None);
        var second = await sut.AuditAndNotifyIfNeededAsync(14, "bot", ct: CancellationToken.None);

        Assert.True(first.IsGracePeriod);
        Assert.True(second.IsGracePeriod);
        _notifications.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("creator", true)]
    [InlineData("member", true)]
    [InlineData("left", false)]
    [InlineData("kicked", false)]
    public void TelegramChannelMembershipChecker_RecognizesActiveStatuses(string status, bool expected)
    {
        Assert.Equal(expected, TelegramChannelMembershipChecker.IsActiveMemberStatus(status));
    }

    [Fact]
    public void IsMergedAccount_RequiresTelegramAndGoogleLinks()
    {
        Assert.True(FreeTierAccessComplianceService.IsMergedAccount(
        [
            new UserIdentityLink { Provider = "telegram", ExternalId = "1" },
            new UserIdentityLink { Provider = "google", ExternalId = "sub" },
        ]));

        Assert.False(FreeTierAccessComplianceService.IsMergedAccount(
        [
            new UserIdentityLink { Provider = "telegram", ExternalId = "1" },
        ]));
    }

    [Fact]
    public void IsMergedAccount_RecognizesTelegramAndLocalLinks()
    {
        Assert.True(FreeTierAccessComplianceService.IsMergedAccount(
        [
            new UserIdentityLink { Provider = "telegram", ExternalId = "1" },
            new UserIdentityLink { Provider = "local", ExternalId = "42" },
        ]));
    }

    [Fact]
    public async Task GetStatusAsync_WhenCompliant_DoesNotNotifyAdmins()
    {
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 10, QuotaPlanId = 2 });
        _quotaPlanQuery
            .Setup(q => q.GetById(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 2, Name = QuotaPlanNames.Default });
        _identityLinkQuery
            .Setup(q => q.GetListByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserIdentityLink { Provider = "telegram", ExternalId = "12345" },
                new UserIdentityLink { Provider = "google", ExternalId = "sub" },
            ]);

        var sut = CreateSut();
        var status = await sut.GetStatusAsync(10, CancellationToken.None);

        Assert.True(status.IsApplicable);
        Assert.True(status.IsCompliant);
        Assert.True(status.IsMergedAccount);
        Assert.Equal("@DataGateVPNBot", status.RequiredChannel);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatusAsync_WhenNotCompliant_ReportsCanRequestLinkCode()
    {
        SetupFreePlanUser(15, 555);
        SetupGraceSettings(enabled: false);
        _identityLinkQuery
            .Setup(q => q.GetListByUserId(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "google", ExternalId = "sub" }]);

        var sut = CreateSut();
        var status = await sut.GetStatusAsync(15, CancellationToken.None);

        Assert.True(status.IsApplicable);
        Assert.False(status.IsCompliant);
        Assert.True(status.CanRequestAccountLinkCode);
        Assert.False(status.IsLinkedToTelegram);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatusAsync_DoesNotStartGracePeriod()
    {
        SetupFreePlanUser(16, 666);
        SetupGraceSettings(enabled: true, minutes: 5);

        var sut = CreateSut();
        var status = await sut.GetStatusAsync(16, CancellationToken.None);

        Assert.True(status.IsApplicable);
        Assert.False(status.IsCompliant);
        Assert.False(status.IsGracePeriod);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatusAsync_WhenGraceActive_ReportsCompliant()
    {
        SetupFreePlanUser(17, 777);
        SetupGraceSettings(enabled: true, minutes: 5);

        var sut = CreateSut();
        await sut.AuditAndNotifyIfNeededAsync(17, "bot", ct: CancellationToken.None);
        var status = await sut.GetStatusAsync(17, CancellationToken.None);

        Assert.True(status.IsApplicable);
        Assert.True(status.IsCompliant);
        Assert.True(status.IsGracePeriod);
    }

    [Fact]
    public async Task AuditByTelegramId_WhenNoIdentityLink_IsNotCompliant()
    {
        _identityLinkQuery
            .Setup(q => q.GetByProviderAndExternalId("telegram", "55555", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityLink?)null);

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededByTelegramIdAsync(55555, "bot", ct: CancellationToken.None);

        Assert.True(result.IsApplicable);
        Assert.False(result.IsCompliant);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AuditByTelegramId_WhenPaidPlan_AllowsViaIsCompliant()
    {
        _identityLinkQuery
            .Setup(q => q.GetByProviderAndExternalId("telegram", "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityLink { UserId = 30, Provider = "telegram", ExternalId = "12345" });
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 30, QuotaPlanId = 3 });
        _quotaPlanQuery
            .Setup(q => q.GetById(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 3, Name = "Pro" });

        var sut = CreateSut();
        var result = await sut.AuditAndNotifyIfNeededByTelegramIdAsync(12345, "bot", ct: CancellationToken.None);

        Assert.False(result.IsApplicable);
        Assert.True(result.IsCompliant);
    }

    [Fact]
    public async Task ShouldEnforceOpenVpnDisconnectAsync_WhenNotSubscribedAndNotMerged_ReturnsTrue()
    {
        SetupFreePlanUser(40, 888);
        _channelChecker.Setup(c => c.IsSubscribedAsync(888, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        SetupGraceSettings(enabled: true, minutes: 5);

        var sut = CreateSut();
        var shouldKill = await sut.ShouldEnforceOpenVpnDisconnectAsync(40, CancellationToken.None);

        Assert.True(shouldKill);
    }

    [Fact]
    public async Task ShouldEnforceOpenVpnDisconnectAsync_WhenChannelSubscribed_ReturnsFalse()
    {
        SetupFreePlanUser(41, 889);
        _channelChecker.Setup(c => c.IsSubscribedAsync(889, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var shouldKill = await sut.ShouldEnforceOpenVpnDisconnectAsync(41, CancellationToken.None);

        Assert.False(shouldKill);
    }

    [Fact]
    public async Task ShouldEnforceOpenVpnDisconnectAsync_IgnoresGracePeriod()
    {
        SetupFreePlanUser(42, 890);
        _channelChecker.Setup(c => c.IsSubscribedAsync(890, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        SetupGraceSettings(enabled: true, minutes: 5);

        var sut = CreateSut();
        await sut.AuditAndNotifyIfNeededAsync(42, "bot", ct: CancellationToken.None);

        var shouldKill = await sut.ShouldEnforceOpenVpnDisconnectAsync(42, CancellationToken.None);

        Assert.True(shouldKill);
    }
}
