using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.Others;

public class TelegramNotifierTests
{
    [Fact]
    public async Task Send_WhenAdminNotLinked_ThrowsWithClearMessage()
    {
        var links = new Mock<IUserIdentityLinkQueryService>(MockBehavior.Strict);
        links.Setup(l => l.GetListByUserId(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var sender = new Mock<ITelegramDirectMessageSender>(MockBehavior.Strict);
        var sut = new TelegramNotifier(links.Object, sender.Object, NullLogger<TelegramNotifier>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Send(new Notification
            {
                Id = 1,
                Title = "T",
                Message = "M",
                Severity = NotificationSeverity.Info,
                Type = "ovpn.issued"
            }, 11, CancellationToken.None));

        Assert.Contains("not linked to Telegram", ex.Message);
    }

    [Fact]
    public async Task Send_WhenLinked_SendsFormattedMessage()
    {
        var links = new Mock<IUserIdentityLinkQueryService>(MockBehavior.Strict);
        links.Setup(l => l.GetListByUserId(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new UserIdentityLink
                {
                    UserId = 11,
                    Provider = AuthIdentityProviders.Telegram,
                    ExternalId = "439938925"
                }
            ]);
        var sender = new Mock<ITelegramDirectMessageSender>(MockBehavior.Strict);
        sender.Setup(s => s.SendMessageAsync(439938925, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TelegramSendOutcome.Ok());
        var sut = new TelegramNotifier(links.Object, sender.Object, NullLogger<TelegramNotifier>.Instance);

        await sut.Send(new Notification
        {
            Id = 1,
            Title = "OpenVPN profile (with token) issued",
            Message = "FileId=4383",
            Severity = NotificationSeverity.Info,
            Type = "ovpn.issued"
        }, 11, CancellationToken.None);

        sender.Verify(s => s.SendMessageAsync(
            439938925,
            It.Is<string>(t => t.Contains("[Info]") && t.Contains("FileId=4383")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
