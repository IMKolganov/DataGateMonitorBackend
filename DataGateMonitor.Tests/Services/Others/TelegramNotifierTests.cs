using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.Others;

public class TelegramNotifierTests
{
    private readonly Mock<IUserIdentityLinkQueryService> _identityLinks = new(MockBehavior.Strict);
    private readonly Mock<ITelegramDirectMessageSender> _dmSender = new(MockBehavior.Strict);

    private static ApplicationDbContext CreateDb(params User[] users)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var config = new ConfigurationBuilder().Build();
        var db = new ApplicationDbContext(options, config);
        if (users.Length > 0)
        {
            db.Users.AddRange(users);
            db.SaveChanges();
        }
        return db;
    }

    private TelegramNotifier CreateSut(ApplicationDbContext? db = null)
        => new(
            _identityLinks.Object,
            db ?? CreateDb(),
            _dmSender.Object,
            NullLogger<TelegramNotifier>.Instance);

    private static Notification SampleNotification()
        => new()
        {
            Id = 42,
            Title = "OpenVPN profile (with token) issued",
            Message = "FileId=4316; FileName=adg-125-x.ovpn; ExternalId=105524505079409950780; TokenId=4051",
            Type = "ovpn.issued",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task Send_WhenTelegramLinkedAndSendSucceeds_SendsTitleAndMessage()
    {
        const int adminUserId = 10;
        _identityLinks.Setup(q => q.GetListByUserId(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "439938925" }]);
        _dmSender.Setup(s => s.SendMessageAsync(439938925, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TelegramSendOutcome.Ok());

        var notification = SampleNotification();
        await CreateSut().Send(notification, adminUserId, CancellationToken.None);

        _dmSender.Verify(
            s => s.SendMessageAsync(
                439938925,
                "OpenVPN profile (with token) issued\nFileId=4316; FileName=adg-125-x.ovpn; ExternalId=105524505079409950780; TokenId=4051",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Send_WhenAdminNotLinked_ThrowsSkippedWithClearMessage()
    {
        const int adminUserId = 11;
        _identityLinks.Setup(q => q.GetListByUserId(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "google", ExternalId = "sub-1" }]);
        await using var db = CreateDb(new User
        {
            Id = adminUserId,
            DisplayName = "Alice Admin",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        });

        var ex = await Assert.ThrowsAsync<NotificationChannelSkippedException>(
            () => CreateSut(db).Send(SampleNotification(), adminUserId, CancellationToken.None));

        Assert.Contains("not linked to Telegram", ex.Message);
        Assert.Contains("Alice Admin", ex.Message);
        Assert.Contains("(11)", ex.Message);
        Assert.DoesNotContain("UserIdentityLink", ex.Message);
        _dmSender.Verify(
            s => s.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Send_WhenSendMessageFails_Throws()
    {
        const int adminUserId = 12;
        _identityLinks.Setup(q => q.GetListByUserId(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = "telegram", ExternalId = "100" }]);
        _dmSender.Setup(s => s.SendMessageAsync(100, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TelegramSendOutcome.Fail("Telegram sendMessage HTTP 403: Forbidden"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut().Send(SampleNotification(), adminUserId, CancellationToken.None));

        Assert.Contains("403", ex.Message);
    }
}
