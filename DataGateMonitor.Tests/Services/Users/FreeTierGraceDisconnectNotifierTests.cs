using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.AdminEmail;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.EmailTemplates;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.Services.Users;
using DataGateMonitor.Services.Users.Interfaces;

namespace DataGateMonitor.Tests.Services.Users;

public class FreeTierGraceDisconnectNotifierTests
{
    private readonly Mock<IUserQueryService> _userQuery = new();
    private readonly Mock<IUserIdentityLinkQueryService> _identityLinkQuery = new();
    private readonly Mock<IFreeTierAccessComplianceService> _complianceService = new();
    private readonly Mock<ITelegramDirectMessageSender> _telegramSender = new();
    private readonly Mock<IEmailSenderService> _emailSender = new();
    private readonly Mock<ISystemTransactionalEmailService> _systemEmail = new();
    private readonly Mock<ISentEmailLogService> _sentEmailLog = new();

    private FreeTierGraceDisconnectNotifier CreateSut()
        => new(
            _userQuery.Object,
            _identityLinkQuery.Object,
            _complianceService.Object,
            _telegramSender.Object,
            _emailSender.Object,
            _systemEmail.Object,
            _sentEmailLog.Object,
            Options.Create(new TelegramChannelSettings { RequiredChannelUsername = "DataGateVPNBot" }),
            Mock.Of<ILogger<FreeTierGraceDisconnectNotifier>>());

    [Fact]
    public async Task NotifyAsync_WhenTelegramLinkedAndSendSucceeds_DoesNotAttemptEmail()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, DisplayName = "u1", Email = "u1@example.com" });
        _identityLinkQuery.Setup(q => q.GetListByUserId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "555" }]);
        _telegramSender.Setup(s => s.TrySendMessageAsync(555, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var outcome = await sut.NotifyAsync(1, ct: CancellationToken.None);

        Assert.Equal("telegram", outcome.Channel);
        Assert.True(outcome.Sent);
        _telegramSender.Verify(s => s.TrySendMessageAsync(555, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(s => s.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_WhenTelegramSendFails_FallsBackToEmail()
    {
        _userQuery.Setup(q => q.GetById(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 2, DisplayName = "u2", Email = "u2@example.com" });
        _identityLinkQuery.Setup(q => q.GetListByUserId(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "556" }]);
        _telegramSender.Setup(s => s.TrySendMessageAsync(556, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _complianceService.Setup(s => s.EvaluateAccessForEnforcementAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FreeTierAccessComplianceResult { IsApplicable = true, ActivePlanName = QuotaPlanNames.Free });
        _systemEmail.Setup(s => s.GetFreeTierGraceDisconnectedAsync(
                QuotaPlanNames.Free, "@DataGateVPNBot", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Subject", "<html/>"));

        var sut = CreateSut();
        var outcome = await sut.NotifyAsync(2, ct: CancellationToken.None);

        Assert.Equal("email", outcome.Channel);
        Assert.True(outcome.Sent);
        _emailSender.Verify(s => s.SendAsync(
            "u2@example.com", "Subject", "<html/>", It.IsAny<CancellationToken>()), Times.Once);
        _sentEmailLog.Verify(s => s.LogAsync(
            2, "u2@example.com", "Subject", "<html/>", true, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_WhenNoTelegramLink_UsesEmailDirectly()
    {
        _userQuery.Setup(q => q.GetById(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 3, DisplayName = "u3", Email = "u3@example.com" });
        _identityLinkQuery.Setup(q => q.GetListByUserId(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "google", ExternalId = "sub" }]);
        _complianceService.Setup(s => s.EvaluateAccessForEnforcementAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FreeTierAccessComplianceResult { IsApplicable = true, ActivePlanName = QuotaPlanNames.Default });
        _systemEmail.Setup(s => s.GetFreeTierGraceDisconnectedAsync(
                QuotaPlanNames.Default, "@DataGateVPNBot", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Subject", "<html/>"));

        var sut = CreateSut();
        var outcome = await sut.NotifyAsync(3, ct: CancellationToken.None);

        Assert.Equal("email", outcome.Channel);
        Assert.True(outcome.Sent);
        _telegramSender.Verify(s => s.TrySendMessageAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(s => s.SendAsync(
            "u3@example.com", "Subject", "<html/>", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_WhenNeitherChannelAvailable_ReturnsNoChannelOutcome()
    {
        _userQuery.Setup(q => q.GetById(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 4, DisplayName = "u4", Email = null });
        _identityLinkQuery.Setup(q => q.GetListByUserId(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut();
        var outcome = await sut.NotifyAsync(4, ct: CancellationToken.None);

        Assert.Null(outcome.Channel);
        Assert.False(outcome.Sent);
        _telegramSender.Verify(s => s.TrySendMessageAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(s => s.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_WhenTelegramFailsAndNoEmail_ReportsTelegramChannelNotSent()
    {
        _userQuery.Setup(q => q.GetById(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, DisplayName = "u7", Email = null });
        _identityLinkQuery.Setup(q => q.GetListByUserId(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "777" }]);
        _telegramSender.Setup(s => s.TrySendMessageAsync(777, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var outcome = await sut.NotifyAsync(7, ct: CancellationToken.None);

        Assert.Equal("telegram", outcome.Channel);
        Assert.False(outcome.Sent);
    }

    [Fact]
    public async Task NotifyAsync_WhenEmailSendThrows_LogsFailureAndReportsEmailChannelNotSent()
    {
        _userQuery.Setup(q => q.GetById(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 8, DisplayName = "u8", Email = "u8@example.com" });
        _identityLinkQuery.Setup(q => q.GetListByUserId(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _complianceService.Setup(s => s.EvaluateAccessForEnforcementAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FreeTierAccessComplianceResult { IsApplicable = true, ActivePlanName = QuotaPlanNames.Free });
        _systemEmail.Setup(s => s.GetFreeTierGraceDisconnectedAsync(
                QuotaPlanNames.Free, "@DataGateVPNBot", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Subject", "<html/>"));
        _emailSender.Setup(s => s.SendAsync("u8@example.com", "Subject", "<html/>", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var sut = CreateSut();
        var outcome = await sut.NotifyAsync(8, ct: CancellationToken.None);

        Assert.Equal("email", outcome.Channel);
        Assert.False(outcome.Sent);
        _sentEmailLog.Verify(s => s.LogAsync(
            8, "u8@example.com", "Subject", "<html/>", false, "smtp down", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_WhenUserNotFound_ReturnsNoChannelOutcome()
    {
        _userQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var sut = CreateSut();
        var outcome = await sut.NotifyAsync(5, ct: CancellationToken.None);

        Assert.Null(outcome.Channel);
        Assert.False(outcome.Sent);
        _identityLinkQuery.Verify(q => q.GetListByUserId(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_WhenDependencyThrows_DoesNotPropagate()
    {
        _userQuery.Setup(q => q.GetById(6, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        var sut = CreateSut();
        FreeTierGraceDisconnectOutcome? outcome = null;
        var exception = await Record.ExceptionAsync(async () => outcome = await sut.NotifyAsync(6, ct: CancellationToken.None));

        Assert.Null(exception);
        Assert.NotNull(outcome);
        Assert.False(outcome!.Sent);
    }
}
