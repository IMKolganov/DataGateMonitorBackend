using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.Services.Others.Notifications.OvpnFileApi;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Others.Notifications.OvpnFileApi;

public class OvpnFileNotificationServiceTests
{
    private readonly Mock<INotificationService> _notifications = new(MockBehavior.Strict);
    private readonly ApplicationDbContext _db;
    private readonly OvpnFileNotificationService _sut;

    public OvpnFileNotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        _db = new ApplicationDbContext(options, new ConfigurationBuilder().Build());
        _sut = new OvpnFileNotificationService(_notifications.Object, _db);
    }

    [Fact]
    public async Task NotifyReadAll_UsesWebAndTelegramChannels()
    {
        IEnumerable<string>? channels = null;
        NotifyAdminsRequest? request = null;
        _notifications
            .Setup(s => s.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>?, CancellationToken>((req, ch, _) =>
            {
                request = req;
                channels = ch;
            })
            .ReturnsAsync(1);

        await _sut.NotifyReadAll(7, 3, CancellationToken.None);

        channels.Should().BeEquivalentTo("web", "telegram");
        request!.PreferenceKind.Should().Be(ApplicationNotificationKind.OpenVpnProfileRead);
        request.Type.Should().Be("ovpn.read.all");
    }

    [Fact]
    public async Task NotifyIssued_UsesWebAndTelegramChannels()
    {
        IEnumerable<string>? channels = null;
        _notifications
            .Setup(s => s.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>?, CancellationToken>((_, ch, _) => channels = ch)
            .ReturnsAsync(1);

        await _sut.NotifyIssued(1, 10, "a.ovpn", "ext", CancellationToken.None);

        channels.Should().BeEquivalentTo("web", "telegram");
    }

    [Fact]
    public async Task NotifyIssued_IncludesDisplayNameWhenIdentityExists()
    {
        NotifyAdminsRequest? request = null;
        _db.Users.Add(new DataGateMonitor.Models.User
        {
            Id = 42,
            DisplayName = "Bob Client",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        });
        _db.UserIdentityLinks.Add(new DataGateMonitor.Models.UserIdentityLink
        {
            Id = 1,
            UserId = 42,
            Provider = "google",
            ExternalId = "ext-bob",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        _notifications
            .Setup(s => s.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>?, CancellationToken>((req, _, _) => request = req)
            .ReturnsAsync(1);

        await _sut.NotifyIssued(1, 10, "a.ovpn", "ext-bob", CancellationToken.None);

        request!.Message.Should().Contain("DisplayName=Bob Client");
        request.Message.Should().Contain("ExternalId=ext-bob");
        request.Message.Should().Contain("FileName=a.ovpn");
    }

    [Fact]
    public async Task NotifyReadAll_Xray_UsesWebAndTelegramChannels()
    {
        IEnumerable<string>? channels = null;
        NotifyAdminsRequest? request = null;
        _notifications
            .Setup(s => s.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>?, CancellationToken>((req, ch, _) =>
            {
                request = req;
                channels = ch;
            })
            .ReturnsAsync(1);

        await _sut.NotifyReadAll(9, 2, CancellationToken.None, VpnProfileNotificationStack.Xray);

        channels.Should().BeEquivalentTo("web", "telegram");
        request!.PreferenceKind.Should().Be(ApplicationNotificationKind.XrayProfileRead);
    }
}
