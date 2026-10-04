using System.Linq.Expressions;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.NotificationRecipientTable;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.Others;

public class NotificationServiceTests
{
    private readonly Mock<ICommandService<Notification, int>> _notificationCmd = new(MockBehavior.Strict);
    private readonly Mock<IUserRoleQueryService> _userRoles = new(MockBehavior.Strict);
    private readonly Mock<ICommandService<NotificationRecipient, int>> _recipientCmd = new(MockBehavior.Strict);
    private readonly Mock<INotificationRecipientQueryService> _recipientQuery = new(MockBehavior.Strict);
    private readonly Mock<IVpnProfileNotificationPreferenceService> _prefs = new(MockBehavior.Strict);

    private NotificationService CreateSut(params INotifier[] notifiers)
    {
        var dict = new Dictionary<string, INotifier>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in notifiers)
            dict[n.Channel] = n;
        return new NotificationService(
            _notificationCmd.Object,
            _userRoles.Object,
            _recipientCmd.Object,
            _recipientQuery.Object,
            dict,
            _prefs.Object,
            NullLogger<NotificationService>.Instance);
    }

    private static Mock<INotifier> CreateNotifier(string channel)
    {
        var mock = new Mock<INotifier>(MockBehavior.Strict);
        mock.SetupGet(n => n.Channel).Returns(channel);
        return mock;
    }

    [Fact]
    public async Task NotifyAdmins_WhenPreferenceDisabled_ReturnsZero_AndDoesNotPersist()
    {
        _prefs.Setup(p => p.IsApplicationNotificationAllowedAsync(ApplicationNotificationKind.OpenVpnProfileMutate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut(CreateNotifier("web").Object);

        var id = await sut.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = "ovpn.issued",
            Title = "T",
            Message = "M",
            Severity = NotificationSeverity.Info,
            PreferenceKind = ApplicationNotificationKind.OpenVpnProfileMutate
        }, ["web", "telegram"], CancellationToken.None);

        Assert.Equal(0, id);
        _notificationCmd.Verify(c => c.Add(It.IsAny<Notification>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyAdmins_WhenTelegramMissing_PersistsFailedTelegramRecipient()
    {
        _prefs.Setup(p => p.IsApplicationNotificationAllowedAsync(It.IsAny<ApplicationNotificationKind>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRoles.Setup(u => u.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([7]);

        _notificationCmd
            .Setup(c => c.Add(It.IsAny<Notification>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, bool _, CancellationToken _) =>
            {
                n.Id = 50472;
                return n;
            });

        List<NotificationRecipient>? saved = null;
        _recipientCmd
            .Setup(c => c.AddRange(It.IsAny<IEnumerable<NotificationRecipient>>(), true, It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<NotificationRecipient>, bool, CancellationToken>((rows, _, _) => saved = rows.ToList())
            .ReturnsAsync(2);

        var web = CreateNotifier("web");
        web.Setup(n => n.Send(It.IsAny<Notification>(), 7, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _recipientCmd
            .Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<NotificationRecipient, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<NotificationRecipient>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut(web.Object); // telegram not registered

        var id = await sut.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = "ovpn.issued",
            Title = "OpenVPN profile (with token) issued",
            Message = "FileId=4383",
            Severity = NotificationSeverity.Info
        }, ["web", "telegram"], CancellationToken.None);

        Assert.Equal(50472, id);
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.Count);
        var tg = saved.Single(r => r.DeliveryChannel == "telegram");
        Assert.Equal(DeliveryStatus.Failed, tg.DeliveryStatus);
        Assert.Contains("not registered", tg.DeliveryError);
        var webRow = saved.Single(r => r.DeliveryChannel == "web");
        Assert.Equal(DeliveryStatus.Pending, webRow.DeliveryStatus);
        web.Verify(n => n.Send(It.IsAny<Notification>(), 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAdmins_WhenTelegramSendThrows_MarksFailedViaUpdateWhere()
    {
        _userRoles.Setup(u => u.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([11]);
        _notificationCmd
            .Setup(c => c.Add(It.IsAny<Notification>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, bool _, CancellationToken _) =>
            {
                n.Id = 100;
                return n;
            });

        _recipientCmd
            .Setup(c => c.AddRange(It.IsAny<IEnumerable<NotificationRecipient>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var tg = CreateNotifier("telegram");
        tg.Setup(n => n.Send(It.IsAny<Notification>(), 11, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Admin user 11 is not linked to Telegram"));

        Expression<Func<NotificationRecipient, bool>>? failedPredicate = null;
        _recipientCmd
            .Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<NotificationRecipient, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<NotificationRecipient>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<NotificationRecipient, bool>>, Action<UpdateSettersBuilder<NotificationRecipient>>, CancellationToken>(
                (pred, _, _) => failedPredicate = pred)
            .ReturnsAsync(1);

        var sut = CreateSut(tg.Object);
        await sut.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = "ovpn.issued",
            Title = "T",
            Message = "M",
            Severity = NotificationSeverity.Info
        }, ["telegram"], CancellationToken.None);

        Assert.NotNull(failedPredicate);
        var compiled = failedPredicate!.Compile();
        Assert.True(compiled(new NotificationRecipient
        {
            NotificationId = 100,
            AdminUserId = 11,
            DeliveryChannel = "telegram"
        }));
        Assert.False(compiled(new NotificationRecipient
        {
            NotificationId = 100,
            AdminUserId = 11,
            DeliveryChannel = "web"
        }));
    }

    [Fact]
    public async Task NotifyAdmins_WhenSendSucceeds_MarksSentViaUpdateWhere()
    {
        _userRoles.Setup(u => u.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([11]);
        _notificationCmd
            .Setup(c => c.Add(It.IsAny<Notification>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, bool _, CancellationToken _) =>
            {
                n.Id = 101;
                return n;
            });
        _recipientCmd
            .Setup(c => c.AddRange(It.IsAny<IEnumerable<NotificationRecipient>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var web = CreateNotifier("web");
        web.Setup(n => n.Send(It.IsAny<Notification>(), 11, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var updateCount = 0;
        _recipientCmd
            .Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<NotificationRecipient, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<NotificationRecipient>>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => updateCount++)
            .ReturnsAsync(1);

        var sut = CreateSut(web.Object);
        await sut.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = "ovpn.issued",
            Title = "T",
            Message = "M",
            Severity = NotificationSeverity.Info
        }, ["web"], CancellationToken.None);

        Assert.Equal(1, updateCount);
        web.Verify(n => n.Send(It.IsAny<Notification>(), 11, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageForUserAsync_MapsDeliveriesIncludingTelegramErrors()
    {
        var request = new GetNotificationsRequest { Page = 1, PageSize = 10 };
        _recipientQuery
            .Setup(q => q.GetNotificationListPageByAdminUserIdAsync(7, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<NotificationListRow>
            {
                Page = 1,
                PageSize = 10,
                TotalCount = 1,
                Items =
                [
                    new NotificationListRow(
                        50472,
                        "ovpn.issued",
                        (int)NotificationSeverity.Info,
                        "OpenVPN profile (with token) issued",
                        "FileId=4383",
                        false,
                        DateTimeOffset.UtcNow,
                        null)
                ]
            });
        _recipientQuery
            .Setup(q => q.GetDeliveriesByAdminUserIdAndNotificationIdsAsync(7, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new NotificationDeliveryRow(50472, "web", DeliveryStatus.Sent, null, DateTimeOffset.UtcNow),
                new NotificationDeliveryRow(50472, "telegram", DeliveryStatus.Failed, "Admin user 7 is not linked to Telegram", null)
            ]);

        var sut = CreateSut();
        var page = await sut.GetPageForUserAsync(7, request, CancellationToken.None);

        var item = Assert.Single(page.Notifications.Items);
        Assert.Equal(50472, item.Id);
        Assert.Equal(2, item.Deliveries.Count);
        Assert.Contains(item.Deliveries, d => d.Channel == "web" && d.Status == DeliveryStatus.Sent);
        Assert.Contains(item.Deliveries, d =>
            d.Channel == "telegram"
            && d.Status == DeliveryStatus.Failed
            && d.Error != null
            && d.Error.Contains("not linked"));
    }

    [Fact]
    public async Task NotifyAdmins_WhenNoAdmins_ReturnsZero()
    {
        _userRoles.Setup(u => u.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var sut = CreateSut(CreateNotifier("web").Object);

        var id = await sut.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = "ovpn.issued",
            Title = "T",
            Message = "M",
            Severity = NotificationSeverity.Info
        }, ["web"], CancellationToken.None);

        Assert.Equal(0, id);
    }
}
