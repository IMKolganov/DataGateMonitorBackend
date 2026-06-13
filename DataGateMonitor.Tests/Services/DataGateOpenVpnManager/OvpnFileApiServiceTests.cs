using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OvpnFileApi;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class OvpnFileApiServiceTests
{
    [Fact]
    public async Task GetByToken_WhenTokenNotFound_ThrowsInvalidOperationException()
    {
        var tokenQuery = new Mock<IIssuedOvpnFileTokenQueryService>(MockBehavior.Strict);
        tokenQuery.Setup(q => q.GetByToken("bad-token", It.IsAny<CancellationToken>())).ReturnsAsync((IssuedOvpnFileToken?)null);

        var sut = CreateService(tokenQuery: tokenQuery);
        var act = () => sut.GetByToken("bad-token", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IssuedOvpnFileToken not found*");
        tokenQuery.VerifyAll();
    }

    [Fact]
    public async Task GetByToken_WhenTokenAndFileExist_ReturnsFile_AndCallsNotify()
    {
        var token = new IssuedOvpnFileToken { Id = 1, IssuedOvpnFileId = 10, Token = "valid-token", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow };
        var file = new IssuedOvpnFile { Id = 10, VpnServerId = 1, CommonName = "cn", FileName = "f.ovpn", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow };
        var tokenQuery = new Mock<IIssuedOvpnFileTokenQueryService>(MockBehavior.Strict);
        tokenQuery.Setup(q => q.GetByToken("valid-token", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        var fileQuery = new Mock<IIssuedOvpnFileQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetByIdAndIsRevoked(10, false, It.IsAny<CancellationToken>())).ReturnsAsync(file);
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyReadByToken("valid-token", 10, 1, false, It.IsAny<CancellationToken>(),
            It.IsAny<VpnProfileNotificationStack>())).Returns(Task.CompletedTask);

        var sut = CreateService(tokenQuery: tokenQuery, fileQuery: fileQuery, notification: notification);
        var result = await sut.GetByToken("valid-token", CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(10);
        result.CommonName.Should().Be("cn");
        notification.Verify(
            n => n.NotifyReadByToken("valid-token", 10, 1, false, It.IsAny<CancellationToken>(),
                VpnProfileNotificationStack.OpenVpn), Times.Once);
    }

    [Fact]
    public async Task GetAllByExternalId_ReturnsFiles_AndCallsNotify()
    {
        var files = new List<IssuedOvpnFile>
        {
            new() { Id = 1, ExternalId = "ext1", VpnServerId = 1, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow },
            new() { Id = 2, ExternalId = "ext1", VpnServerId = 1, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };
        var fileQuery = new Mock<IIssuedOvpnFileQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetAllByExternalId("ext1", It.IsAny<CancellationToken>())).ReturnsAsync(files);
        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyReadByExternalId("ext1", 2, It.IsAny<CancellationToken>(),
            It.IsAny<VpnProfileNotificationStack>())).Returns(Task.CompletedTask);

        var sut = CreateService(fileQuery: fileQuery, notification: notification);
        var result = await sut.GetAllByExternalId("ext1", CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].ExternalId.Should().Be("ext1");
        notification.Verify(
            n => n.NotifyReadByExternalId("ext1", 2, It.IsAny<CancellationToken>(), VpnProfileNotificationStack.OpenVpn),
            Times.Once);
    }

    [Fact]
    public async Task DownloadOvpnFileByCn_WhenDbRowMissingThenFound_RetriesAndSucceeds()
    {
        var server = new VpnServer
        {
            Id = 75,
            ServerType = VpnServerType.OpenVpn,
            ServerName = "s75",
            ApiUrl = "https://vpn.test/",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var file = new IssuedOvpnFile
        {
            Id = 1369,
            VpnServerId = 75,
            CommonName = "adg-75-local:232-test",
            FileName = "adg-75-local:232-test.ovpn",
            FilePath = "/pki/ovpn_files/adg-75-local:232-test.ovpn",
            ExternalId = "local:232",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(75, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        var fileQuery = new Mock<IIssuedOvpnFileQueryService>(MockBehavior.Strict);
        fileQuery.SetupSequence(q => q.GetByCommonNameAndVpnServerIdAndIsRevoked(
                "adg-75-local:232-test", 75, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuedOvpnFile?)null)
            .ReturnsAsync(file);

        var ovpnClient = new Mock<IOvpnFileApiClient>(MockBehavior.Strict);
        ovpnClient.Setup(c => c.DownloadOvpnFile(
                75,
                It.Is<DownloadOvpnFileRequest>(r => r.CommonName == file.CommonName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OvpnFileDownload
            {
                CommonName = file.CommonName,
                FileName = file.FileName,
                Content = new byte[] { 1, 2, 3 }
            });

        var notification = new Mock<IOvpnFileNotificationService>(MockBehavior.Strict);
        notification.Setup(n => n.NotifyDownloaded(
                75, file.FileName, file.ExternalId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService(
            ovpnClient: ovpnClient,
            fileQuery: fileQuery,
            serverQuery: serverQuery,
            notification: notification);

        var result = await sut.DownloadOvpnFileByCn(
            new DownloadFileByCnRequest { VpnServerId = 75, CommonName = "adg-75-local:232-test" },
            CancellationToken.None);

        result.Content.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        fileQuery.Verify(
            q => q.GetByCommonNameAndVpnServerIdAndIsRevoked(
                "adg-75-local:232-test", 75, false, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        ovpnClient.Verify(
            c => c.DownloadOvpnFile(75, It.IsAny<DownloadOvpnFileRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DownloadOvpnFileByCn_WhenAllAttemptsFail_ThrowsAfterFiveDbLookups()
    {
        var server = new VpnServer
        {
            Id = 75,
            ServerType = VpnServerType.OpenVpn,
            ServerName = "s75",
            ApiUrl = "https://vpn.test/",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

        var serverQuery = new Mock<IVpnServerQueryService>(MockBehavior.Strict);
        serverQuery.Setup(q => q.GetById(75, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        var fileQuery = new Mock<IIssuedOvpnFileQueryService>(MockBehavior.Strict);
        fileQuery.Setup(q => q.GetByCommonNameAndVpnServerIdAndIsRevoked(
                "missing-cn", 75, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuedOvpnFile?)null);

        var sut = CreateService(fileQuery: fileQuery, serverQuery: serverQuery);
        var act = () => sut.DownloadOvpnFileByCn(
            new DownloadFileByCnRequest { VpnServerId = 75, CommonName = "missing-cn" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Issued OVPN file not found*");
        fileQuery.Verify(
            q => q.GetByCommonNameAndVpnServerIdAndIsRevoked(
                "missing-cn", 75, false, It.IsAny<CancellationToken>()),
            Times.Exactly(5));
    }

    private static OvpnFileApiService CreateService(
        Mock<IOvpnFileApiClient>? ovpnClient = null,
        Mock<IIssuedOvpnFileTokenQueryService>? tokenQuery = null,
        Mock<IIssuedOvpnFileQueryService>? fileQuery = null,
        Mock<IVpnServerQueryService>? serverQuery = null,
        Mock<IOvpnFileNotificationService>? notification = null)
    {
        ovpnClient ??= new Mock<IOvpnFileApiClient>(MockBehavior.Loose);
        var logger = Mock.Of<ILogger<OvpnFileApiService>>();
        var config = new ConfigurationBuilder().Build();
        var configQuery = new Mock<IVpnServerOvpnFileConfigQueryService>(MockBehavior.Loose);
        serverQuery ??= new Mock<IVpnServerQueryService>(MockBehavior.Loose);
        var fileCommand = new Mock<ICommandService<IssuedOvpnFile, int>>(MockBehavior.Loose);
        var tokenCommand = new Mock<ICommandService<IssuedOvpnFileToken, int>>(MockBehavior.Loose);

        tokenQuery ??= new Mock<IIssuedOvpnFileTokenQueryService>(MockBehavior.Loose);
        fileQuery ??= new Mock<IIssuedOvpnFileQueryService>(MockBehavior.Loose);
        notification ??= new Mock<IOvpnFileNotificationService>(MockBehavior.Loose);

        return new OvpnFileApiService(
            ovpnClient.Object,
            logger,
            config,
            configQuery.Object,
            fileQuery.Object,
            tokenQuery.Object,
            fileCommand.Object,
            tokenCommand.Object,
            serverQuery.Object,
            notification.Object);
    }
}
