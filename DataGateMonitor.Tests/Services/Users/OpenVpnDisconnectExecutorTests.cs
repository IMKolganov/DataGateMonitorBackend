using DataGateMonitor.DataBase.Services.Command.Interfaces;
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

    public OpenVpnDisconnectExecutorTests()
    {
        // Mirrors EfCommandService<T,TKey>.Add: assigns an id and returns the same entity.
        var nextId = 1;
        _disconnectLogCommandService
            .Setup(x => x.Add(It.IsAny<FreeTierDisconnectLog>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FreeTierDisconnectLog entity, bool _, CancellationToken _) =>
            {
                entity.Id = nextId++;
                return entity;
            });
    }

    private OpenVpnDisconnectExecutor CreateSut()
        => new(
            _openVpnClientService.Object,
            _issuedOvpnFileQueryService.Object,
            _ovpnFileApiService.Object,
            _disconnectLogCommandService.Object,
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
    public async Task ExecuteWithLogIdAsync_ReturnsIdOfWrittenLogRow()
    {
        var sut = CreateSut();
        var request = new OpenVpnDisconnectRequest
        {
            Server = Server(),
            Client = Client(),
            UserId = 5,
            Reason = DisconnectReason.Enforcement,
            RevokeCertificate = false,
        };

        var (response, logId) = await sut.ExecuteWithLogIdAsync(request, CancellationToken.None);

        Assert.True(response.Success);
        Assert.NotNull(logId);
        Assert.Equal(1, logId);
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
    public async Task UpdateNotificationOutcomeAsync_UpdatesRowById()
    {
        var sut = CreateSut();
        await sut.UpdateNotificationOutcomeAsync(42, "telegram", true, CancellationToken.None);

        _disconnectLogCommandService.Verify(
            x => x.UpdateWhere(
                It.IsAny<System.Linq.Expressions.Expression<Func<FreeTierDisconnectLog, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<FreeTierDisconnectLog>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateNotificationOutcomeAsync_WhenCommandServiceThrows_DoesNotPropagate()
    {
        _disconnectLogCommandService
            .Setup(x => x.UpdateWhere(
                It.IsAny<System.Linq.Expressions.Expression<Func<FreeTierDisconnectLog, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<FreeTierDisconnectLog>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db unavailable"));

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() =>
            sut.UpdateNotificationOutcomeAsync(42, "email", false, CancellationToken.None));

        Assert.Null(exception);
    }
}
