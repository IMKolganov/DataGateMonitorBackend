using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Tests.Hubs;

public class AdminNotificationHubServiceTests
{
    [Fact]
    public async Task SendNotificationAsync_Sends_To_Admin_Group_With_ReceiveNotification()
    {
        // Arrange
        var adminUserId = 101;
        var notification = new Notification();
        var ct = CancellationToken.None;

        var hubContext = new Mock<IHubContext<AdminNotificationHub>>();
        var hubClients = new Mock<IHubClients>();
        var groupClient = new Mock<IClientProxy>();
        var logger = new Mock<ILogger<AdminNotificationHubService>>();

        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);
        hubClients.Setup(c => c.Group($"admin-{adminUserId}")).Returns(groupClient.Object);

        groupClient
            .Setup(c => c.SendCoreAsync(
                It.Is<string>(m => m == "ReceiveNotification"),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = new AdminNotificationHubService(hubContext.Object, logger.Object);

        // Act
        await sut.SendNotificationAsync(adminUserId, notification, ct);

        // Assert
        hubClients.Verify(c => c.Group($"admin-{adminUserId}"), Times.Once);
        groupClient.Verify(
            c => c.SendCoreAsync(
                It.Is<string>(m => m == "ReceiveNotification"),
                It.Is<object?[]>(args => args.Length >= 1 && ReferenceEquals(args[0], notification)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_Logs_Error_When_Send_Fails()
    {
        // Arrange
        var adminUserId = 202;
        var notification = new Notification();
        var ct = CancellationToken.None;

        var hubContext = new Mock<IHubContext<AdminNotificationHub>>();
        var hubClients = new Mock<IHubClients>();
        var groupClient = new Mock<IClientProxy>();
        var logger = new Mock<ILogger<AdminNotificationHubService>>();

        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);
        hubClients.Setup(c => c.Group($"admin-{adminUserId}")).Returns(groupClient.Object);

        var expectedException = new InvalidOperationException("send failed");
        groupClient
            .Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var sut = new AdminNotificationHubService(hubContext.Object, logger.Object);

        // Act: method should swallow exceptions and log error
        await sut.SendNotificationAsync(adminUserId, notification, ct);

        // Assert: verify LogError was called once
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("Failed to send notification via SignalR")
                                                 && state!.ToString()!.Contains("202")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
