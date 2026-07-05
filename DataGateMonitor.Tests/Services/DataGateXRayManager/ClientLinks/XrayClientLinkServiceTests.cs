using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTable;
using DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTokenTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.Services.Others.Notifications.OvpnFileApi;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.DataGateXRayManager.ClientLinks;

public class XrayClientLinkServiceTests
{
    [Fact]
    public async Task GetAllByExternalId_WithTelegramId_ReturnsRowsFromAllLinkedIdentityIds()
    {
        const string telegramId = "123456789";
        const string googleSub = "accounts.google.com:sub-xray";
        var googleLink = new IssuedXrayClientLink
        {
            Id = 1,
            ExternalId = googleSub,
            VpnServerId = 2,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        };
        var legacyLink = new IssuedXrayClientLink
        {
            Id = 2,
            ExternalId = telegramId,
            VpnServerId = 2,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        };

        var linkQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        linkQuery.Setup(q => q.GetAllByExternalId(googleSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync([googleLink]);
        linkQuery.Setup(q => q.GetAllByExternalId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([legacyLink]);

        var identityLinkQuery = CreateMergedUserIdentityLinkQuery(telegramId, googleSub);
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyReadByExternalId(
                googleSub, 2, It.IsAny<CancellationToken>(), VpnProfileNotificationStack.Xray))
            .Returns(Task.CompletedTask);

        var sut = CreateService(linkQuery: linkQuery, notification: notification, identityLinkQuery: identityLinkQuery);
        var result = await sut.GetAllByExternalId(telegramId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(l => l.Id).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task GetAllByExternalIdAndVpnServerId_WithTelegramId_QueriesBothLinkedIdentityIds()
    {
        const string telegramId = "444555666";
        const string googleSub = "google-sub-xray-server";
        const int vpnServerId = 9;

        var xrayServer = new VpnServer
        {
            Id = vpnServerId,
            ServerType = VpnServerType.Xray,
            ServerName = "xray-9",
            ApiUrl = "https://xray.test/",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        };

        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(vpnServerId, It.IsAny<CancellationToken>())).ReturnsAsync(xrayServer);

        var linkQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        linkQuery.Setup(q => q.GetAllByVpnServerIdAndExternalIdAndIsRevoked(
                vpnServerId, googleSub, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        linkQuery.Setup(q => q.GetAllByVpnServerIdAndExternalIdAndIsRevoked(
                vpnServerId, telegramId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new IssuedXrayClientLink
                {
                    Id = 7,
                    ExternalId = telegramId,
                    VpnServerId = vpnServerId,
                    CreateDate = DateTimeOffset.UtcNow,
                    LastUpdate = DateTimeOffset.UtcNow,
                },
            ]);

        var identityLinkQuery = CreateMergedUserIdentityLinkQuery(telegramId, googleSub);
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyReadByExternalIdAndVpnServerId(
                vpnServerId, googleSub, 1, false, It.IsAny<CancellationToken>(), VpnProfileNotificationStack.Xray))
            .Returns(Task.CompletedTask);

        var sut = CreateService(
            linkQuery: linkQuery,
            serverQuery: serverQuery,
            notification: notification,
            identityLinkQuery: identityLinkQuery);

        var result = await sut.GetAllByExternalIdAndVpnServerId(vpnServerId, telegramId, CancellationToken.None);

        result.Should().ContainSingle(l => l.Id == 7);
    }

    [Fact]
    public async Task AddClientLink_WithTelegramId_PersistsGoogleSub()
    {
        const string telegramId = "777888999";
        const string googleSub = "accounts.google.com:sub-xray-add";
        const int vpnServerId = 4;
        const string commonName = "xray-bot-profile";

        var xrayServer = new VpnServer
        {
            Id = vpnServerId,
            ServerType = VpnServerType.Xray,
            ServerName = "xray-4",
            ApiUrl = "https://xray.test/",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        };
        var exportConfig = new VpnServerOvpnFileConfig
        {
            VpnServerId = vpnServerId,
            ConfigTemplate = "vless-template",
            VpnServerIp = "vpn.example.com",
            VpnServerPort = 443,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        };

        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(vpnServerId, It.IsAny<CancellationToken>())).ReturnsAsync(xrayServer);

        var configQuery = new Mock<IVpnServerOvpnFileConfigQueryService>(MockBehavior.Strict);
        configQuery.Setup(q => q.GetByVpnServerIdId(vpnServerId, It.IsAny<CancellationToken>())).ReturnsAsync(exportConfig);

        var linkQuery = new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Strict);
        linkQuery.Setup(q => q.ExistsActiveByVpnServerIdAndCommonName(vpnServerId, commonName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        linkQuery.Setup(q => q.GetByVpnServerIdAndCommonName(51, vpnServerId, commonName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, int _, string _, CancellationToken _) => new IssuedXrayClientLink
            {
                Id = id,
                ExternalId = googleSub,
                CommonName = commonName,
                VpnServerId = vpnServerId,
                CreateDate = DateTimeOffset.UtcNow,
                LastUpdate = DateTimeOffset.UtcNow,
            });

        var microserviceClient = new Mock<IXrayClientLinkMicroserviceClient>(MockBehavior.Strict);
        microserviceClient.Setup(c => c.AddClientLink(
                vpnServerId,
                It.Is<GenerateClientLinkMicroserviceRequest>(r => r.CommonName == commonName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientLinkMetadataDto
            {
                CommonName = commonName,
                FileName = $"{commonName}.txt",
                FilePath = $"/links/{commonName}.txt",
                IssuedAt = DateTime.UtcNow,
                IssuedTo = "bot",
            });

        IssuedXrayClientLink? captured = null;
        var linkCommand = new Mock<ICommandService<IssuedXrayClientLink, int>>(MockBehavior.Strict);
        linkCommand.Setup(c => c.Add(It.IsAny<IssuedXrayClientLink>(), true, It.IsAny<CancellationToken>()))
            .Callback<IssuedXrayClientLink, bool, CancellationToken>((entity, _, _) => captured = entity)
            .ReturnsAsync((IssuedXrayClientLink entity, bool _, CancellationToken _) =>
            {
                entity.Id = 51;
                return entity;
            });

        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyIssued(
                vpnServerId, 51, $"{commonName}.txt", googleSub, It.IsAny<CancellationToken>(),
                VpnProfileNotificationStack.Xray))
            .Returns(Task.CompletedTask);

        var identityLinkQuery = CreateMergedUserIdentityLinkQuery(telegramId, googleSub);
        var sut = CreateService(
            linkQuery: linkQuery,
            serverQuery: serverQuery,
            configQuery: configQuery,
            microserviceClient: microserviceClient,
            linkCommand: linkCommand,
            notification: notification,
            identityLinkQuery: identityLinkQuery);

        var request = new AddFileRequest
        {
            VpnServerId = vpnServerId,
            CommonName = commonName,
            ExternalId = telegramId,
            IssuedTo = $"telegram user {telegramId}",
        };

        var result = await sut.AddClientLink(request, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ExternalId.Should().Be(googleSub);
        request.ExternalId.Should().Be(googleSub);
        result.ExternalId.Should().Be(googleSub);
    }

    private static Mock<IUserIdentityLinkQueryService> CreateMergedUserIdentityLinkQuery(
        string telegramId,
        string googleSub,
        int userId = 10)
    {
        var identityLinkQuery = new Mock<IUserIdentityLinkQueryService>(MockBehavior.Strict);
        identityLinkQuery.Setup(q => q.GetByExternalId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityLink
            {
                UserId = userId,
                Provider = AuthIdentityProviders.Telegram,
                ExternalId = telegramId,
            });
        identityLinkQuery.Setup(q => q.GetByExternalId(googleSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityLink
            {
                UserId = userId,
                Provider = AuthIdentityProviders.Google,
                ExternalId = googleSub,
            });
        identityLinkQuery.Setup(q => q.GetListByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new UserIdentityLink { Provider = AuthIdentityProviders.Telegram, ExternalId = telegramId },
                new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = googleSub },
            ]);
        return identityLinkQuery;
    }

    private static XrayClientLinkService CreateService(
        Mock<IXrayClientLinkMicroserviceClient>? microserviceClient = null,
        Mock<IIssuedXrayClientLinkQueryService>? linkQuery = null,
        Mock<IIssuedXrayClientLinkTokenQueryService>? tokenQuery = null,
        Mock<IVpnServerOvpnFileConfigQueryService>? configQuery = null,
        Mock<ICommandService<IssuedXrayClientLink, int>>? linkCommand = null,
        Mock<IVpnServerQueryService>? serverQuery = null,
        Mock<IOvpnFileNotificationService>? notification = null,
        Mock<IUserIdentityLinkQueryService>? identityLinkQuery = null)
    {
        microserviceClient ??= new Mock<IXrayClientLinkMicroserviceClient>(MockBehavior.Loose);
        var logger = Mock.Of<ILogger<XrayClientLinkService>>();
        var config = new ConfigurationBuilder().Build();
        configQuery ??= new Mock<IVpnServerOvpnFileConfigQueryService>(MockBehavior.Loose);
        linkQuery ??= new Mock<IIssuedXrayClientLinkQueryService>(MockBehavior.Loose);
        tokenQuery ??= new Mock<IIssuedXrayClientLinkTokenQueryService>(MockBehavior.Loose);
        linkCommand ??= new Mock<ICommandService<IssuedXrayClientLink, int>>(MockBehavior.Loose);
        var tokenCommand = new Mock<ICommandService<IssuedXrayClientLinkToken, int>>(MockBehavior.Loose);
        serverQuery ??= new Mock<IVpnServerQueryService>(MockBehavior.Loose);
        notification ??= new Mock<IOvpnFileNotificationService>(MockBehavior.Loose);
        identityLinkQuery ??= new Mock<IUserIdentityLinkQueryService>(MockBehavior.Loose);

        return new XrayClientLinkService(
            microserviceClient.Object,
            logger,
            config,
            configQuery.Object,
            linkQuery.Object,
            tokenQuery.Object,
            identityLinkQuery.Object,
            linkCommand.Object,
            tokenCommand.Object,
            serverQuery.Object,
            notification.Object);
    }
}
