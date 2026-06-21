using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class NotificationControllerTests
{
    private readonly Mock<INotificationService> notificationServiceMock;
    private readonly NotificationController controller;

    public NotificationControllerTests()
    {
        notificationServiceMock = new Mock<INotificationService>(MockBehavior.Strict);
        controller = new NotificationController(notificationServiceMock.Object);
    }

    [Fact]
    public async Task NotifyAdminsAsync_ReturnsOk_WithSuccessResponse_AndCallsService()
    {
        // Arrange
        var request = new NotifyAdminsRequest
        {
            Type = "test",
            Title = "Test Title",
            Message = "Hello"
        };

        var expectedId = 123;
        var ct = CancellationToken.None;

        notificationServiceMock
            .Setup(s => s.NotifyAdmins(request, It.Is<IEnumerable<string>>(c => c.SequenceEqual(new[] { "web" })), ct))
            .ReturnsAsync(expectedId);

        // Act
        var result = await controller.NotifyAdminsAsync(request, ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<int>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.Equal(expectedId, response.Data);

        notificationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task MarkDeliveredAsync_ReturnsOk_WithSuccessResponse_AndCallsService()
    {
        // Arrange
        var notificationId = 5;
        var adminUserId = 7;
        var channel = "web";
        var ct = CancellationToken.None;

        notificationServiceMock
            .Setup(s => s.MarkDelivered(notificationId, adminUserId, channel, ct))
            .Returns(Task.CompletedTask);

        // Act
        var result = await controller.MarkDeliveredAsync(notificationId, adminUserId, channel, ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.True(response.Data);

        notificationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task MarkReadAsync_ReturnsOk_WithSuccessResponse_AndCallsService()
    {
        // Arrange
        var notificationId = 9;
        var adminUserId = 11;
        var ct = CancellationToken.None;

        notificationServiceMock
            .Setup(s => s.MarkRead(notificationId, adminUserId, ct))
            .Returns(Task.CompletedTask);

        // Act
        var result = await controller.MarkReadAsync(notificationId, adminUserId, ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.True(response.Data);

        notificationServiceMock.VerifyAll();
    }
}
