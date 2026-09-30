using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateMonitor.Tests.Services.Others;

public class TelegramNotifierTests
{
    private readonly Mock<IUserIdentityLinkQueryService> _identityLinks = new();
    private readonly Mock<ITelegramDirectMessageSender> _dmSender = new();

    private TelegramNotifier CreateSut()
        => new(
            _identityLinks.Object,
            _dmSender.Object,
            Mock.Of<ILogger<TelegramNotifier>>());

    private static Notification SampleNotification()
        => new()
        {
            Id = 42,
            Title = "New user registered",
            Message = "User #7 (Alice) registered. Via Google.",
            Type = "user.registered",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task Send_WhenTelegramLinkedAndSendSucceeds_SendsTitleAndMessage()
    {
        const int adminUserId = 10;
        _identityLinks.Setup(q => q.GetListByUserId(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "439938925" }]);
        _dmSender.Setup(s => s.TrySendMessageAsync(439938925, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var notification = SampleNotification();
        await CreateSut().Send(notification, adminUserId, CancellationToken.None);

        _dmSender.Verify(
            s => s.TrySendMessageAsync(
                439938925,
                "New user registered\nUser #7 (Alice) registered. Via Google.",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Send_WhenNoTelegramLink_SkipsWithoutSending()
    {
        const int adminUserId = 11;
        _identityLinks.Setup(q => q.GetListByUserId(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "google", ExternalId = "sub-1" }]);

        await CreateSut().Send(SampleNotification(), adminUserId, CancellationToken.None);

        _dmSender.Verify(
            s => s.TrySendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Send_WhenTrySendReturnsFalse_Throws()
    {
        const int adminUserId = 12;
        _identityLinks.Setup(q => q.GetListByUserId(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "100" }]);
        _dmSender.Setup(s => s.TrySendMessageAsync(100, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut().Send(SampleNotification(), adminUserId, CancellationToken.None));

        Assert.Contains("42", ex.Message);
        Assert.Contains("12", ex.Message);
    }
}
