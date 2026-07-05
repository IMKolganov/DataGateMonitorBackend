using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
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

    private FreeTierAccessComplianceService CreateSut()
        => new(
            _quotaAssignmentQuery.Object,
            _quotaPlanQuery.Object,
            _identityLinkQuery.Object,
            _channelChecker.Object,
            _notifications.Object,
            Options.Create(new TelegramChannelSettings { RequiredChannelUsername = "DataGateVPNBot" }),
            Mock.Of<ILogger<FreeTierAccessComplianceService>>());

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
        _quotaAssignmentQuery
            .Setup(q => q.GetActiveByUserId(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 12, QuotaPlanId = 1 });
        _quotaPlanQuery
            .Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 1, Name = QuotaPlanNames.Free });
        _identityLinkQuery
            .Setup(q => q.GetListByUserId(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "888" }]);
        _channelChecker
            .Setup(c => c.IsSubscribedAsync(888, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
}
