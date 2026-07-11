using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using DataGateMonitor.Services.Users;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataGateMonitor.Tests.Services.Users;

public class OpenVpnDisconnectExecutorTests
{
    private readonly Mock<IOpenVpnClientService> _openVpnClientService = new();
    private readonly Mock<IIssuedOvpnFileQueryService> _issuedOvpnFileQueryService = new();
    private readonly Mock<IOvpnFileApiService> _ovpnFileApiService = new();
    private readonly Mock<ICommandService<FreeTierDisconnectLog, int>> _disconnectLogCommandService = new();
    private readonly Mock<IQueryService<FreeTierDisconnectLog, int>> _disconnectLogQueryService = new();

    private OpenVpnDisconnectExecutor CreateSut()
        => new(
            _openVpnClientService.Object,
            _issuedOvpnFileQueryService.Object,
            _ovpnFileApiService.Object,
            _disconnectLogCommandService.Object,
            _disconnectLogQueryService.Object,
            Mock.Of<ILogger<OpenVpnDisconnectExecutor>>());

    private static VpnServer Server(int id = 1) => new() { Id = id, ServerName = "srv-1" };

    private static VpnServerClient Client(string cn = "client-1", long? managementClientId = 7)
        => new() { CommonName = cn, ManagementClientId = managementClientId };

    [Fact]
    public async Task ExecuteAsync_KillsAndLogs_WithoutRevoke_WhenRevokeNotRequested()
    {
        var sut = CreateSut();
        var request = new OpenVpnDisconnectRequest
        {
            Server = Server(),
            Client = Client(),
            UserId = 5,
            UserDisplayNameSnapshot = "Alice",
            Reason = DisconnectReason.Manual,
            InitiatedByUserId = 9,
            RevokeCertificate = false,
        };

        var result = await sut.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(result.RevokeAttempted);
        Assert.Null(result.RevokeSucceeded);
        Assert.Null(result.ErrorMessage);

        _openVpnClientService.Verify(
            x => x.KillConnectedClientAsync(request.Server, request.Client, It.IsAny<CancellationToken>()),
            Times.Once);
        _ovpnFileApiService.Verify(
            x => x.RevokeOvpnFile(It.IsAny<RevokeFileRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _disconnectLogCommandService.Verify(
            x => x.Add(
                It.Is<FreeTierDisconnectLog>(l =>
                    l.UserId == 5 &&
                    l.UserDisplayNameSnapshot == "Alice" &&
                    l.VpnServerId == 1 &&
                    l.CommonName == "client-1" &&
                    l.Reason == (int)DisconnectReason.Manual &&
                    l.InitiatedByUserId == 9 &&
                    l.RevokeRequested == false &&
                    l.KillSucceeded == true),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RevokesActiveIssuedFile_WhenRevokeRequested()
    {
        var server = Server();
        _issuedOvpnFileQueryService
            .Setup(x => x.GetByCommonNameAndVpnServerIdAndIsRevoked("client-1", server.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedOvpnFile { Id = 77, CommonName = "client-1", VpnServerId = server.Id });
        _ovpnFileApiService
            .Setup(x => x.RevokeOvpnFile(It.IsAny<RevokeFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedOvpnFile { Id = 77, IsRevoked = true });

        var sut = CreateSut();
        var request = new OpenVpnDisconnectRequest
        {
            Server = server,
            Client = Client(),
            Reason = DisconnectReason.Enforcement,
            RevokeCertificate = true,
        };

        var result = await sut.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.RevokeAttempted);
        Assert.True(result.RevokeSucceeded);

        _ovpnFileApiService.Verify(
            x => x.RevokeOvpnFile(
                It.Is<RevokeFileRequest>(r => r.OvpnFileId == 77 && r.VpnServerId == server.Id && r.CommonName == "client-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RevokeFails_WhenNoActiveIssuedFileFound()
    {
        var server = Server();
        _issuedOvpnFileQueryService
            .Setup(x => x.GetByCommonNameAndVpnServerIdAndIsRevoked("client-1", server.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuedOvpnFile?)null);

        var sut = CreateSut();
        var request = new OpenVpnDisconnectRequest
        {
            Server = server,
            Client = Client(),
            Reason = DisconnectReason.Manual,
            RevokeCertificate = true,
        };

        var result = await sut.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.RevokeAttempted);
        Assert.False(result.RevokeSucceeded);
        Assert.NotNull(result.ErrorMessage);
        _ovpnFileApiService.Verify(
            x => x.RevokeOvpnFile(It.IsAny<RevokeFileRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_KillFailure_StillWritesLog_AndReturnsFailure()
    {
        _openVpnClientService
            .Setup(x => x.KillConnectedClientAsync(It.IsAny<VpnServer>(), It.IsAny<VpnServerClient>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("management interface unreachable"));

        var sut = CreateSut();
        var request = new OpenVpnDisconnectRequest
        {
            Server = Server(),
            Client = Client(),
            Reason = DisconnectReason.Enforcement,
            RevokeCertificate = false,
        };

        var result = await sut.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("management interface unreachable", result.ErrorMessage);

        _disconnectLogCommandService.Verify(
            x => x.Add(
                It.Is<FreeTierDisconnectLog>(l => l.KillSucceeded == false),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateNotificationOutcomeAsync_WhenMatchingLogFound_UpdatesIt()
    {
        var entry = new FreeTierDisconnectLog
        {
            Id = 42,
            UserId = 5,
            VpnServerId = 1,
            CommonName = "client-1",
            Reason = (int)DisconnectReason.Enforcement,
        };
        _disconnectLogQueryService
            .Setup(q => q.FirstOrDefault(
                It.IsAny<System.Linq.Expressions.Expression<Func<FreeTierDisconnectLog, bool>>>(),
                It.IsAny<Func<IQueryable<FreeTierDisconnectLog>, IOrderedQueryable<FreeTierDisconnectLog>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<FreeTierDisconnectLog, object>>[]>()))
            .ReturnsAsync(entry);

        var sut = CreateSut();
        await sut.UpdateNotificationOutcomeAsync(5, 1, "client-1", DisconnectReason.Enforcement, "telegram", true, CancellationToken.None);

        _disconnectLogCommandService.Verify(
            x => x.Update(
                It.Is<FreeTierDisconnectLog>(l => l.Id == 42 && l.NotificationChannel == "telegram" && l.NotificationSent),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateNotificationOutcomeAsync_WhenNoMatchingLogFound_DoesNotThrowOrUpdate()
    {
        _disconnectLogQueryService
            .Setup(q => q.FirstOrDefault(
                It.IsAny<System.Linq.Expressions.Expression<Func<FreeTierDisconnectLog, bool>>>(),
                It.IsAny<Func<IQueryable<FreeTierDisconnectLog>, IOrderedQueryable<FreeTierDisconnectLog>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<FreeTierDisconnectLog, object>>[]>()))
            .ReturnsAsync((FreeTierDisconnectLog?)null);

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() =>
            sut.UpdateNotificationOutcomeAsync(5, 1, "client-1", DisconnectReason.Enforcement, "email", false, CancellationToken.None));

        Assert.Null(exception);
        _disconnectLogCommandService.Verify(
            x => x.Update(It.IsAny<FreeTierDisconnectLog>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
