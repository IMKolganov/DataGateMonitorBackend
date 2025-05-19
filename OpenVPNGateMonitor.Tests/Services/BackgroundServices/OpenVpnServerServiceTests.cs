using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.BackgroundServices;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.OpenVpnTelnet;

namespace OpenVPNGateMonitor.Tests.Services.BackgroundServices;

public class OpenVpnServerServiceTests
{
    private readonly Mock<ILogger<IOpenVpnServerService>> _loggerMock;
    private readonly Mock<IOpenVpnClientService> _openVpnClientServiceMock;
    private readonly Mock<IOpenVpnSummaryStatService> _openVpnSummaryStatServiceMock;
    private readonly Mock<IOpenVpnVersionService> _openVpnVersionServiceMock;
    private readonly Mock<IOpenVpnStateService> _openVpnStateServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IExternalIpAddressService> _externalIpAddressServiceMock; // Changed to mock
    private readonly OpenVpnServerService _service;

    public OpenVpnServerServiceTests()
    {
        _loggerMock = new Mock<ILogger<IOpenVpnServerService>>();
        _openVpnClientServiceMock = new Mock<IOpenVpnClientService>();
        _openVpnSummaryStatServiceMock = new Mock<IOpenVpnSummaryStatService>();
        _openVpnVersionServiceMock = new Mock<IOpenVpnVersionService>();
        _openVpnStateServiceMock = new Mock<IOpenVpnStateService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _externalIpAddressServiceMock = new Mock<IExternalIpAddressService>(); // Initialize mock

        _service = new OpenVpnServerService(
            _loggerMock.Object,
            _openVpnClientServiceMock.Object,
            _openVpnSummaryStatServiceMock.Object,
            _openVpnVersionServiceMock.Object,
            _openVpnStateServiceMock.Object,
            _unitOfWorkMock.Object,
            _externalIpAddressServiceMock.Object // Use mock object
        );
    }

    [Fact]
    public async Task SaveConnectedClientsAsync_WhenExceptionOccurs_ShouldRollbackTransaction()
    {
        // Arrange
        var vpnServerId = 1;
        var commandQueue = Mock.Of<ICommandQueue>();
        var cancellationToken = CancellationToken.None;
        var mockTransaction = new Mock<IDbContextTransaction>();

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(cancellationToken))
            .ReturnsAsync(mockTransaction.Object);
        _openVpnClientServiceMock.Setup(x => x.GetClientsAsync(commandQueue, cancellationToken))
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.SaveConnectedClientsAsync(vpnServerId, commandQueue, cancellationToken));

        mockTransaction.Verify(x => x.RollbackAsync(cancellationToken), Times.Once);
        mockTransaction.Verify(x => x.CommitAsync(cancellationToken), Times.Never);
    }

    [Fact]
    public void GenerateSessionId_ShouldGenerateConsistentIds()
    {
        // Arrange
        var commonName = "TestClient";
        var realAddress = "192.168.1.1";
        var connectedSince = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sessionId1 = _service.GetType()
            .GetMethod("GenerateSessionId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_service, new object[] { commonName, realAddress, connectedSince }) as Guid?;

        var sessionId2 = _service.GetType()
            .GetMethod("GenerateSessionId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_service, new object[] { commonName, realAddress, connectedSince }) as Guid?;

        // Assert
        Assert.NotNull(sessionId1);
        Assert.NotNull(sessionId2);
        Assert.Equal(sessionId1.Value, sessionId2.Value);
    }

    [Fact]
    public async Task SaveConnectedClientsAsync_WhenCalledTwice_UpdatesExistingClientInsteadOfAdding()
    {
        // Arrange
        var vpnServerId = 1;
        var connectedSince = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var commonName = "client1 external_123456";
        var remoteIp = "10.0.0.1";

        var sessionString = $"{commonName}-{remoteIp}-{connectedSince:o}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionString));
        var sessionId = new Guid(hashBytes.Take(16).ToArray());

        var fakeClient = new OpenVpnServerClient
        {
            CommonName = commonName,
            RemoteIp = remoteIp,
            LocalIp = "192.168.0.1",
            BytesReceived = 100,
            BytesSent = 200,
            ConnectedSince = connectedSince,
            Username = "user1",
            Country = "CY",
            Region = "Nicosia",
            City = "Nicosia",
            Latitude = 35.1856,
            Longitude = 33.3823,
            VpnServerId = vpnServerId
        };

        var clientList = new List<OpenVpnServerClient> { fakeClient };

        var mockClientService = new Mock<IOpenVpnClientService>();
        mockClientService
            .Setup(s => s.GetClientsAsync(It.IsAny<ICommandQueue>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientList);

        var mockRepo = new Mock<IRepository<OpenVpnServerClient>>();
        var mockUoW = new Mock<IUnitOfWork>();

        var telegramUsers = new List<TelegramBotUser>
        {
            new TelegramBotUser { TelegramId = 123456 }
        };
        mockUoW.Setup(u => u.GetQuery<TelegramBotUser>().AsQueryable())
            .Returns(telegramUsers.AsQueryable().BuildMock());

        mockUoW.SetupSequence(u => u.GetQuery<OpenVpnServerClient>().AsQueryable())
            .Returns(new List<OpenVpnServerClient>().AsQueryable().BuildMock()) // SetDisconnectForAllUsers
            .Returns(new List<OpenVpnServerClient>().AsQueryable().BuildMock()) // foreach (1st)
            .Returns(new List<OpenVpnServerClient>().AsQueryable().BuildMock()) // FirstOrDefault (1st)
            .Returns(new List<OpenVpnServerClient>
                    { new OpenVpnServerClient { SessionId = sessionId, VpnServerId = vpnServerId } }.AsQueryable()
                .BuildMock()) // foreach (2nd)
            .Returns(new List<OpenVpnServerClient>
                    { new OpenVpnServerClient { SessionId = sessionId, VpnServerId = vpnServerId } }.AsQueryable()
                .BuildMock()); // FirstOrDefault (2nd)

        mockUoW.Setup(u => u.GetRepository<OpenVpnServerClient>())
            .Returns(mockRepo.Object);

        var mockTransaction = new Mock<IDbContextTransaction>();
        mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        mockUoW.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);

        mockUoW.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new OpenVpnServerService(
            Mock.Of<ILogger<IOpenVpnServerService>>(),
            mockClientService.Object,
            Mock.Of<IOpenVpnSummaryStatService>(),
            Mock.Of<IOpenVpnVersionService>(),
            Mock.Of<IOpenVpnStateService>(),
            mockUoW.Object,
            Mock.Of<IExternalIpAddressService>()
        );

        var commandQueue = Mock.Of<ICommandQueue>();
        var cancellationToken = CancellationToken.None;

        // Act
        await service.SaveConnectedClientsAsync(vpnServerId, commandQueue, cancellationToken);
        await service.SaveConnectedClientsAsync(vpnServerId, commandQueue, cancellationToken);

        // Assert
        mockRepo.Verify(r => r.AddAsync(It.IsAny<OpenVpnServerClient>(), It.IsAny<CancellationToken>()), Times.Once);
        mockRepo.Verify(r => r.Update(It.Is<OpenVpnServerClient>(c =>
            c.SessionId == sessionId &&
            c.IsConnected == true &&
            c.BytesReceived == fakeClient.BytesReceived
        )), Times.Once);
    }

}