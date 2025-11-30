using Microsoft.AspNetCore.SignalR;
using Moq;
using OpenVPNGateMonitor.Hubs;

namespace OpenVPNGateMonitor.Tests.Hubs;

public class AdminNotificationHubTests
{
    [Fact]
    public async Task JoinAdminGroup_Adds_Connection_To_Admin_Group()
    {
        // Arrange
        var connectionId = "conn-123";
        var adminUserId = 42;

        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns(connectionId);

        var groups = new Mock<IGroupManager>();
        groups
            .Setup(g => g.AddToGroupAsync(connectionId, $"admin-{adminUserId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var hub = new AdminNotificationHub
        {
            Context = context.Object,
            Groups = groups.Object
        };

        // Act
        await hub.JoinAdminGroup(adminUserId);

        // Assert
        groups.Verify(
            g => g.AddToGroupAsync(connectionId, $"admin-{adminUserId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveAdminGroup_Removes_Connection_From_Admin_Group()
    {
        // Arrange
        var connectionId = "conn-456";
        var adminUserId = 77;

        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns(connectionId);

        var groups = new Mock<IGroupManager>();
        groups
            .Setup(g => g.RemoveFromGroupAsync(connectionId, $"admin-{adminUserId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var hub = new AdminNotificationHub
        {
            Context = context.Object,
            Groups = groups.Object
        };

        // Act
        await hub.LeaveAdminGroup(adminUserId);

        // Assert
        groups.Verify(
            g => g.RemoveFromGroupAsync(connectionId, $"admin-{adminUserId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
