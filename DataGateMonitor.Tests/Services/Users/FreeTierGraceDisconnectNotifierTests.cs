using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
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

    private FreeTierGraceDisconnectNotifier CreateSut()
        => new(
            _userQuery.Object,
            _identityLinkQuery.Object,
            _complianceService.Object,
            _telegramSender.Object,
            _emailSender.Object,
            _systemEmail.Object,
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
        await sut.NotifyAsync(1, CancellationToken.None);

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
        await sut.NotifyAsync(2, CancellationToken.None);

        _emailSender.Verify(s => s.SendAsync(
            "u2@example.com", "Subject", "<html/>", It.IsAny<CancellationToken>()), Times.Once);
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
        await sut.NotifyAsync(3, CancellationToken.None);

        _telegramSender.Verify(s => s.TrySendMessageAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(s => s.SendAsync(
            "u3@example.com", "Subject", "<html/>", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_WhenNeitherChannelAvailable_DoesNotThrow()
    {
        _userQuery.Setup(q => q.GetById(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 4, DisplayName = "u4", Email = null });
        _identityLinkQuery.Setup(q => q.GetListByUserId(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut();
        await sut.NotifyAsync(4, CancellationToken.None);

        _telegramSender.Verify(s => s.TrySendMessageAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(s => s.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_WhenUserNotFound_DoesNotThrow()
    {
        _userQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var sut = CreateSut();
        await sut.NotifyAsync(5, CancellationToken.None);

        _identityLinkQuery.Verify(q => q.GetListByUserId(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyAsync_WhenDependencyThrows_DoesNotPropagate()
    {
        _userQuery.Setup(q => q.GetById(6, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() => sut.NotifyAsync(6, CancellationToken.None));

        Assert.Null(exception);
    }
}
