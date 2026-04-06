using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Notifications.Requests;
using OpenVPNGateMonitor.Tests.Helpers;
using Xunit;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.NotificationRecipientTable;

public class NotificationRecipientQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    }

    private static DbContextOptions<TestDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static (NotificationRecipientQueryService sut, TestDbContext ctx) CreateSutWithContext()
    {
        var ctx = new TestDbContext(CreateOptions());
        var queries = new Dictionary<Type, object>
        {
            [typeof(Notification)] = new TestQuery<Notification>(ctx.Notifications),
            [typeof(NotificationRecipient)] = new TestQuery<NotificationRecipient>(ctx.NotificationRecipients)
        };
        var uow = new TestUnitOfWork(queries);
        var recipientQuery = new EfQueryService<NotificationRecipient, int>(uow);
        var notificationQuery = new EfQueryService<Notification, int>(uow);
        var sut = new NotificationRecipientQueryService(recipientQuery, notificationQuery);
        return (sut, ctx);
    }

    [Fact]
    public async Task GetNotificationListByAdminUserIdAsync_Returns_OrderedByCreateDateDesc_WithIsReadAndReadAt()
    {
        var (sut, ctx) = CreateSutWithContext();
        var baseTime = new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
        await ctx.Notifications.AddRangeAsync(
            new Notification { Id = 1, Type = "n1", Severity = NotificationSeverity.Info, Title = "Title1", Message = "M1", CreateDate = baseTime, LastUpdate = baseTime },
            new Notification { Id = 2, Type = "n2", Severity = NotificationSeverity.Warning, Title = "Title2", Message = "M2", CreateDate = baseTime.AddMinutes(1), LastUpdate = baseTime.AddMinutes(1) },
            new Notification { Id = 3, Type = "n3", Severity = NotificationSeverity.Error, Title = "Title3", Message = "M3", CreateDate = baseTime.AddMinutes(2), LastUpdate = baseTime.AddMinutes(2) }
        );
        await ctx.NotificationRecipients.AddRangeAsync(
            new NotificationRecipient { Id = 10, NotificationId = 1, AdminUserId = 100, ReadAt = null, CreateDate = baseTime, LastUpdate = baseTime },
            new NotificationRecipient { Id = 11, NotificationId = 2, AdminUserId = 100, ReadAt = baseTime.AddHours(1), CreateDate = baseTime, LastUpdate = baseTime },
            new NotificationRecipient { Id = 12, NotificationId = 3, AdminUserId = 100, ReadAt = null, CreateDate = baseTime, LastUpdate = baseTime }
        );
        await ctx.SaveChangesAsync();

        var list = await sut.GetNotificationListByAdminUserIdAsync(100, CancellationToken.None);

        list.Should().HaveCount(3);
        list[0].Id.Should().Be(3);
        list[0].Title.Should().Be("Title3");
        list[0].IsRead.Should().BeFalse();
        list[0].ReadAt.Should().BeNull();
        list[1].Id.Should().Be(2);
        list[1].IsRead.Should().BeTrue();
        list[1].ReadAt.Should().NotBeNull();
        list[2].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetNotificationListByAdminUserIdAsync_Returns_OnlyForGivenAdminUserId()
    {
        var (sut, ctx) = CreateSutWithContext();
        var t = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await ctx.Notifications.AddRangeAsync(
            new Notification { Id = 1, Type = "a", Severity = NotificationSeverity.Info, Title = "A", Message = "A", CreateDate = t, LastUpdate = t },
            new Notification { Id = 2, Type = "b", Severity = NotificationSeverity.Info, Title = "B", Message = "B", CreateDate = t, LastUpdate = t }
        );
        await ctx.NotificationRecipients.AddRangeAsync(
            new NotificationRecipient { Id = 1, NotificationId = 1, AdminUserId = 10, CreateDate = t, LastUpdate = t },
            new NotificationRecipient { Id = 2, NotificationId = 2, AdminUserId = 20, CreateDate = t, LastUpdate = t }
        );
        await ctx.SaveChangesAsync();

        var list = await sut.GetNotificationListByAdminUserIdAsync(10, CancellationToken.None);

        list.Should().HaveCount(1);
        list[0].Id.Should().Be(1);
        list[0].Title.Should().Be("A");
    }

    [Fact]
    public async Task GetNotificationListPageByAdminUserIdAsync_Returns_PagedResult()
    {
        var (sut, ctx) = CreateSutWithContext();
        var t = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 1; i <= 5; i++)
        {
            await ctx.Notifications.AddAsync(new Notification { Id = i, Type = $"t{i}", Severity = NotificationSeverity.Info, Title = $"T{i}", Message = "M", CreateDate = t.AddMinutes(i), LastUpdate = t });
            await ctx.NotificationRecipients.AddAsync(new NotificationRecipient { Id = i, NotificationId = i, AdminUserId = 1, CreateDate = t, LastUpdate = t });
        }
        await ctx.SaveChangesAsync();

        var page = await sut.GetNotificationListPageByAdminUserIdAsync(1, new GetNotificationsRequest { Page = 2, PageSize = 2 }, CancellationToken.None);

        page.Page.Should().Be(2);
        page.PageSize.Should().Be(2);
        page.TotalCount.Should().Be(5);
        page.Items.Should().HaveCount(2);
        page.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { 3, 2 }); // desc by CreateDate: 5,4,3,2,1 -> page2 = 3,2
    }

    [Fact]
    public async Task GetNotificationListPageByAdminUserIdAsync_NormalizesPageAndPageSize_WhenLessThanOne()
    {
        var (sut, ctx) = CreateSutWithContext();
        var t = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await ctx.Notifications.AddAsync(new Notification { Id = 1, Type = "x", Severity = NotificationSeverity.Info, Title = "X", Message = "X", CreateDate = t, LastUpdate = t });
        await ctx.NotificationRecipients.AddAsync(new NotificationRecipient { Id = 1, NotificationId = 1, AdminUserId = 1, CreateDate = t, LastUpdate = t });
        await ctx.SaveChangesAsync();

        var page = await sut.GetNotificationListPageByAdminUserIdAsync(1, new GetNotificationsRequest { Page = 0, PageSize = 0 }, CancellationToken.None);

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUnreadCountByAdminUserIdAsync_Returns_CountOfUnreadNotifications()
    {
        var (sut, ctx) = CreateSutWithContext();
        var t = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await ctx.Notifications.AddRangeAsync(
            new Notification { Id = 1, Type = "a", Severity = NotificationSeverity.Info, Title = "A", Message = "A", CreateDate = t, LastUpdate = t },
            new Notification { Id = 2, Type = "b", Severity = NotificationSeverity.Info, Title = "B", Message = "B", CreateDate = t, LastUpdate = t },
            new Notification { Id = 3, Type = "c", Severity = NotificationSeverity.Info, Title = "C", Message = "C", CreateDate = t, LastUpdate = t }
        );
        await ctx.NotificationRecipients.AddRangeAsync(
            new NotificationRecipient { Id = 1, NotificationId = 1, AdminUserId = 50, ReadAt = null, CreateDate = t, LastUpdate = t },
            new NotificationRecipient { Id = 2, NotificationId = 2, AdminUserId = 50, ReadAt = t, CreateDate = t, LastUpdate = t },
            new NotificationRecipient { Id = 3, NotificationId = 3, AdminUserId = 50, ReadAt = null, CreateDate = t, LastUpdate = t }
        );
        await ctx.SaveChangesAsync();

        var count = await sut.GetUnreadCountByAdminUserIdAsync(50, CancellationToken.None);

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetUnreadCountByAdminUserIdAsync_Returns_Zero_WhenNoRecipients()
    {
        var (sut, _) = CreateSutWithContext();

        var count = await sut.GetUnreadCountByAdminUserIdAsync(999, CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetNotificationListPageByAdminUserIdAsync_FiltersBySeverities()
    {
        var (sut, ctx) = CreateSutWithContext();
        var t = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await ctx.Notifications.AddRangeAsync(
            new Notification { Id = 1, Type = "a", Severity = NotificationSeverity.Info, Title = "I", Message = "M", CreateDate = t, LastUpdate = t },
            new Notification { Id = 2, Type = "b", Severity = NotificationSeverity.Warning, Title = "W", Message = "M", CreateDate = t.AddMinutes(1), LastUpdate = t.AddMinutes(1) },
            new Notification { Id = 3, Type = "c", Severity = NotificationSeverity.Error, Title = "E", Message = "M", CreateDate = t.AddMinutes(2), LastUpdate = t.AddMinutes(2) }
        );
        await ctx.NotificationRecipients.AddRangeAsync(
            new NotificationRecipient { Id = 1, NotificationId = 1, AdminUserId = 1, CreateDate = t, LastUpdate = t },
            new NotificationRecipient { Id = 2, NotificationId = 2, AdminUserId = 1, CreateDate = t, LastUpdate = t },
            new NotificationRecipient { Id = 3, NotificationId = 3, AdminUserId = 1, CreateDate = t, LastUpdate = t }
        );
        await ctx.SaveChangesAsync();

        var page = await sut.GetNotificationListPageByAdminUserIdAsync(1,
            new GetNotificationsRequest
            {
                Page = 1,
                PageSize = 10,
                Severities =
                [
                    NotificationSeverity.Warning,
                    NotificationSeverity.Error,
                    NotificationSeverity.Critical
                ]
            },
            CancellationToken.None);

        page.TotalCount.Should().Be(2);
        page.Items.Select(x => x.Id).Should().Equal(3, 2);
    }

    [Fact]
    public async Task GetNotificationListPageByAdminUserIdAsync_FiltersByIsRead()
    {
        var (sut, ctx) = CreateSutWithContext();
        var t = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await ctx.Notifications.AddRangeAsync(
            new Notification { Id = 1, Type = "a", Severity = NotificationSeverity.Info, Title = "A", Message = "M", CreateDate = t, LastUpdate = t },
            new Notification { Id = 2, Type = "b", Severity = NotificationSeverity.Info, Title = "B", Message = "M", CreateDate = t.AddMinutes(1), LastUpdate = t.AddMinutes(1) }
        );
        await ctx.NotificationRecipients.AddRangeAsync(
            new NotificationRecipient { Id = 1, NotificationId = 1, AdminUserId = 1, ReadAt = null, CreateDate = t, LastUpdate = t },
            new NotificationRecipient { Id = 2, NotificationId = 2, AdminUserId = 1, ReadAt = t, CreateDate = t, LastUpdate = t }
        );
        await ctx.SaveChangesAsync();

        var page = await sut.GetNotificationListPageByAdminUserIdAsync(1,
            new GetNotificationsRequest { Page = 1, PageSize = 10, IsRead = false },
            CancellationToken.None);

        page.TotalCount.Should().Be(1);
        page.Items[0].Id.Should().Be(1);
        page.Items[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetNotificationListPageByAdminUserIdAsync_FiltersByType()
    {
        var (sut, ctx) = CreateSutWithContext();
        var t = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await ctx.Notifications.AddRangeAsync(
            new Notification { Id = 1, Type = "server.down", Severity = NotificationSeverity.Critical, Title = "Down", Message = "M", CreateDate = t.AddMinutes(2), LastUpdate = t.AddMinutes(2) },
            new Notification { Id = 2, Type = "server.up", Severity = NotificationSeverity.Info, Title = "Up", Message = "M", CreateDate = t.AddMinutes(1), LastUpdate = t.AddMinutes(1) }
        );
        await ctx.NotificationRecipients.AddRangeAsync(
            new NotificationRecipient { Id = 1, NotificationId = 1, AdminUserId = 1, CreateDate = t, LastUpdate = t },
            new NotificationRecipient { Id = 2, NotificationId = 2, AdminUserId = 1, CreateDate = t, LastUpdate = t }
        );
        await ctx.SaveChangesAsync();

        var page = await sut.GetNotificationListPageByAdminUserIdAsync(1,
            new GetNotificationsRequest { Page = 1, PageSize = 10, Type = "server.up" },
            CancellationToken.None);

        page.TotalCount.Should().Be(1);
        page.Items[0].Id.Should().Be(2);
        page.Items[0].Type.Should().Be("server.up");
    }

}
