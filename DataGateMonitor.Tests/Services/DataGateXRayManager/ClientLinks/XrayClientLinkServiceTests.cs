using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTable;
using DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTokenTable;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.Services.Query.UserVpnServerAccessRuleTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.Services.Others.Notifications.OvpnFileApi;
using DataGateMonitor.Services.VpnAccess;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.DataGateXRayManager.ClientLinks;

public class XrayClientLinkServiceTests
{
    [Fact]
    public async Task GetByToken_WhenTokenNotFound_ThrowsInvalidOperationException()
    {
        var tokenQuery = new Mock<IIssuedXrayClientLinkTokenQueryService>(MockBehavior.Strict);
        tokenQuery.Setup(q => q.GetByToken("bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuedXrayClientLinkToken?)null);

        var sut = CreateService(tokenQuery: tokenQuery);
        var act = () => sut.GetByToken("bad-token", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IssuedXrayClientLinkToken not found*");
        tokenQuery.VerifyAll();
    }

    [Fact]
    public async Task GetByToken_WhenTokenAndLinkExist_ReturnsLink_AndNotifiesXrayStack()
    {
        var token = new IssuedXrayClientLinkToken
        {
            Id = 1,
            IssuedXrayClientLinkId = 10,
            Token = "valid-token",
            CreatedAt = DateTimeOffset.UtcNow,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var link = new IssuedXrayClientLink
        {
            Id = 10,
            VpnServerId = 1,
            CommonName = "cn",
            FileName = "f.txt",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var tokenQuery = new Mock<IIssuedXrayClientLinkTokenQueryService>(MockBehavior.Strict);
        tokenQuery.Setup(q => q.GetByToken("valid-token", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetByIdAndIsRevoked(10, false, It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyReadByToken("valid-token", 10, 1, false, It.IsAny<CancellationToken>(),
            VpnProfileNotificationStack.Xray)).Returns(Task.CompletedTask);

        var sut = CreateService(tokenQuery: tokenQuery, fileQuery: fileQuery, notification: notification);
        var result = await sut.GetByToken("valid-token", CancellationToken.None);

        result.Id.Should().Be(10);
        result.CommonName.Should().Be("cn");
        notification.Verify(
            n => n.NotifyReadByToken("valid-token", 10, 1, false, It.IsAny<CancellationToken>(),
                VpnProfileNotificationStack.Xray), Times.Once);
    }

    [Fact]
    public async Task GetAllByVpnServerId_WhenServerIsOpenVpn_Throws()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer
            {
                Id = 5,
                ServerName = "Helsinki",
                ServerType = VpnServerType.OpenVpn,
                CreateDate = DateTimeOffset.UtcNow,
                LastUpdate = DateTimeOffset.UtcNow
            });

        var sut = CreateService(serverQuery: serverQuery);
        var act = () => sut.GetAllByVpnServerId(5, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only supported for Xray servers*");
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task GetAllByVpnServerId_WhenServerIsXray_ReturnsLinks_AndNotifiesXrayStack()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer
            {
                Id = 5,
                ServerName = "Frankfurt",
                ServerType = VpnServerType.Xray,
                CreateDate = DateTimeOffset.UtcNow,
                LastUpdate = DateTimeOffset.UtcNow
            });
        var links = new List<IssuedXrayClientLink>
        {
            new()
            {
                Id = 2,
                VpnServerId = 5,
                CommonName = "cn",
                CreateDate = DateTimeOffset.UtcNow,
                LastUpdate = DateTimeOffset.UtcNow
            }
        };
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetAllByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(links);
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyReadAll(5, 1, It.IsAny<CancellationToken>(), VpnProfileNotificationStack.Xray))
            .Returns(Task.CompletedTask);

        var sut = CreateService(serverQuery: serverQuery, fileQuery: fileQuery, notification: notification);
        var result = await sut.GetAllByVpnServerId(5, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].CommonName.Should().Be("cn");
        notification.VerifyAll();
        fileQuery.VerifyAll();
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task GetAllByVpnServerId_WhenServerMissing_Throws()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServer?)null);

        var sut = CreateService(serverQuery: serverQuery);
        var act = () => sut.GetAllByVpnServerId(99, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99*not found*");
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task DownloadClientLink_WhenFound_ReturnsXrayDownloadResponse()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer
            {
                Id = 5,
                ServerName = "Frankfurt",
                ServerType = VpnServerType.Xray,
                CreateDate = DateTimeOffset.UtcNow,
                LastUpdate = DateTimeOffset.UtcNow
            });
        var link = new IssuedXrayClientLink
        {
            Id = 11,
            VpnServerId = 5,
            CommonName = "cn",
            FileName = "link.txt",
            FilePath = "/p",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetByIdAndVpnServerIdAndIsRevoked(11, 5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        var micro = new Mock<IXrayClientLinkMicroserviceClient>(MockBehavior.Strict);
        micro.Setup(m => m.DownloadClientLink(5,
                It.IsAny<DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Requests.DownloadClientLinkRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Responses.ClientLinkDownload
            {
                FileName = "link.txt",
                Content = [1, 2, 3]
            });
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyDownloaded(5, "link.txt", It.IsAny<string>(), false,
            It.IsAny<CancellationToken>(), VpnProfileNotificationStack.Xray)).Returns(Task.CompletedTask);

        var sut = CreateService(serverQuery: serverQuery, fileQuery: fileQuery, notification: notification,
            micro: micro);
        var result = await sut.DownloadClientLink(
            new DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests.DownloadXrayClientLinkRequest
            {
                IssuedXrayClientLinkId = 11,
                VpnServerId = 5
            },
            CancellationToken.None);

        result.IssuedXrayClientLink.Id.Should().Be(11);
        result.IssuedXrayClientLink.FileName.Should().Be("link.txt");
        result.Content.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        result.FileSizeBytes.Should().Be(3);
        micro.VerifyAll();
        notification.VerifyAll();
    }

    [Fact]
    public async Task AddClientLink_WhenActiveCommonNameExists_Throws()
    {
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.ExistsActiveByVpnServerIdAndCommonName(5, "cn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateService(fileQuery: fileQuery);
        var act = () => sut.AddClientLink(
            new DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests.AddXrayClientLinkRequest
            {
                VpnServerId = 5,
                ExternalId = "ext",
                CommonName = "cn",
                IssuedTo = "xrayClient"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active client link with the same CommonName already exists*");
        fileQuery.VerifyAll();
    }

    [Fact]
    public async Task AddClientLink_WhenServerIsOpenVpn_Throws()
    {
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.ExistsActiveByVpnServerIdAndCommonName(5, "cn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer
            {
                Id = 5,
                ServerName = "Helsinki",
                ServerType = VpnServerType.OpenVpn,
                CreateDate = DateTimeOffset.UtcNow,
                LastUpdate = DateTimeOffset.UtcNow
            });

        var sut = CreateService(fileQuery: fileQuery, serverQuery: serverQuery);
        var act = () => sut.AddClientLink(
            new DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests.AddXrayClientLinkRequest
            {
                VpnServerId = 5,
                ExternalId = "ext",
                CommonName = "cn",
                IssuedTo = "xrayClient"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only supported for Xray servers*");
        fileQuery.VerifyAll();
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task RevokeClientLink_WhenLinkNotFound_Throws()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(XrayServer());
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetByIdAndVpnServerIdAndCommonNameAndIsRevoked(
                5, 11, "cn", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuedXrayClientLink?)null);

        var sut = CreateService(serverQuery: serverQuery, fileQuery: fileQuery);
        var act = () => sut.RevokeClientLink(
            new DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests.RevokeXrayClientLinkRequest
            {
                VpnServerId = 5,
                IssuedXrayClientLinkId = 11,
                CommonName = "cn"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found for revocation*");
        fileQuery.VerifyAll();
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task DownloadClientLink_WhenNotFound_Throws()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(XrayServer());
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetByIdAndVpnServerIdAndIsRevoked(11, 5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuedXrayClientLink?)null);

        var sut = CreateService(serverQuery: serverQuery, fileQuery: fileQuery);
        var act = () => sut.DownloadClientLink(
            new DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests.DownloadXrayClientLinkRequest
            {
                IssuedXrayClientLinkId = 11,
                VpnServerId = 5
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Issued Xray client link not found. LinkId=11*");
        fileQuery.VerifyAll();
        serverQuery.VerifyAll();
    }

    [Fact]
    public async Task DownloadClientLinkByCn_WhenFound_ReturnsXrayDownloadResponse()
    {
        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(XrayServer());
        var link = new IssuedXrayClientLink
        {
            Id = 11,
            VpnServerId = 5,
            CommonName = "cn",
            FileName = "link.txt",
            FilePath = "/p",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var fileQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetByCommonNameAndVpnServerIdAndIsRevoked("cn", 5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        var micro = new Mock<IXrayClientLinkMicroserviceClient>(MockBehavior.Strict);
        micro.Setup(m => m.DownloadClientLink(5,
                It.IsAny<DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Requests.DownloadClientLinkRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Responses.ClientLinkDownload
            {
                FileName = "link.txt",
                Content = [9, 8]
            });
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyDownloaded(5, "link.txt", It.IsAny<string>(), false,
            It.IsAny<CancellationToken>(), VpnProfileNotificationStack.Xray)).Returns(Task.CompletedTask);

        var sut = CreateService(serverQuery: serverQuery, fileQuery: fileQuery, notification: notification,
            micro: micro);
        var result = await sut.DownloadClientLinkByCn(
            new DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests.DownloadXrayClientLinkByCnRequest
            {
                CommonName = "cn",
                VpnServerId = 5
            },
            CancellationToken.None);

        result.IssuedXrayClientLink.Id.Should().Be(11);
        result.IssuedXrayClientLink.CommonName.Should().Be("cn");
        result.Content.Should().BeEquivalentTo(new byte[] { 9, 8 });
        result.FileSizeBytes.Should().Be(2);
        micro.VerifyAll();
        notification.VerifyAll();
    }

    private static VpnServer XrayServer(int id = 5, string name = "Frankfurt") =>
        new()
        {
            Id = id,
            ServerName = name,
            ServerType = VpnServerType.Xray,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

    private static XrayClientLinkService CreateService(
        Mock<IIssuedXrayClientLinkTokenQueryService>? tokenQuery = null,
        Mock<IIssuedXrayClientLinkQueryService>? fileQuery = null,
        Mock<IVpnServerQueryService>? serverQuery = null,
        Mock<IOvpnFileNotificationService>? notification = null,
        Mock<IXrayClientLinkMicroserviceClient>? micro = null)
    {
        tokenQuery ??= new Mock<IIssuedXrayClientLinkTokenQueryService>(MockBehavior.Loose);
        fileQuery ??= new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Loose);
        serverQuery ??= new Mock<IVpnServerQueryService>(MockBehavior.Loose);
        notification ??= new Mock<IOvpnFileNotificationService>(MockBehavior.Loose);
        micro ??= new Mock<IXrayClientLinkMicroserviceClient>(MockBehavior.Loose);

        var identityLinkQuery = new Mock<IUserIdentityLinkQueryService>(MockBehavior.Loose);
        IVpnServerQuotaPlanAccessGuard accessGuard = new VpnServerQuotaPlanAccessGuard(
            identityLinkQuery.Object,
            new Mock<IUserQuotaPlanQueryService>(MockBehavior.Loose).Object,
            new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Loose).Object,
            new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Loose).Object,
            new Mock<IUserQueryService>(MockBehavior.Loose).Object);

        return new XrayClientLinkService(
            micro.Object,
            Mock.Of<ILogger<XrayClientLinkService>>(),
            new ConfigurationBuilder().Build(),
            new Mock<IVpnServerOvpnFileConfigQueryService>(MockBehavior.Loose).Object,
            fileQuery.Object,
            tokenQuery.Object,
            identityLinkQuery.Object,
            accessGuard,
            new Mock<ICommandService<IssuedXrayClientLink, int>>(MockBehavior.Loose).Object,
            new Mock<ICommandService<IssuedXrayClientLinkToken, int>>(MockBehavior.Loose).Object,
            serverQuery.Object,
            notification.Object);
    }
}
