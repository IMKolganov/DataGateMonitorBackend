using FluentAssertions;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Mapping.TelegramBotUser.Mappings;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.TelegramBotUser.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class TelegramBotUserControllerTests
{
    private readonly Mock<ITelegramUserService> _telegramUserServiceMock;
    private readonly TelegramBotUserController _controller;

    public TelegramBotUserControllerTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(TelegramBotUserMapping).Assembly);
        _telegramUserServiceMock = new Mock<ITelegramUserService>();
        _controller = new TelegramBotUserController(_telegramUserServiceMock.Object);
    }

    [Fact]
    public async Task RegisterUser_ReturnsOkResult_WithRegisterUserResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            TelegramId = 123456789,
            Username = "testuser",
            FirstName = "Test",
            LastName = "User"
        };

        var createdUser = request.Adapt<TelegramBotUser>();
        createdUser.Id = 1;

        _telegramUserServiceMock
            .Setup(s => s.RegisterUserAsync(It.IsAny<TelegramBotUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.RegisterUser(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var apiResponse = okResult!.Value as ApiResponse<RegisterUserResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.TelegramId.Should().Be(request.TelegramId);
        apiResponse.Data.Username.Should().Be(request.Username);
    }

    [Fact]
    public async Task GetAdmins_ReturnsOkResult_WithListOfAdmins()
    {
        // Arrange
        var admins = new List<TelegramBotUser>
        {
            new() { Id = 1, TelegramId = 111, Username = "admin1", FirstName = "Admin", LastName = "One", IsAdmin = true },
            new() { Id = 2, TelegramId = 222, Username = "admin2", FirstName = "Admin", LastName = "Two", IsAdmin = true }
        };

        _telegramUserServiceMock
            .Setup(s => s.GetAdminsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(admins);

        // Act
        var result = await _controller.GetAdmins();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var apiResponse = okResult!.Value as ApiResponse<GetAdminsResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.TelegramBotAdmins.Should().HaveCount(2);
        apiResponse.Data.TelegramBotAdmins[0].Username.Should().Be("admin1");
    }
    
    [Fact]
    public void Map_TelegramBotUser_List_To_GetAdminsResponse_ShouldWork()
    {
        // Arrange
        var admins = new List<TelegramBotUser>
        {
            new() { Id = 1, TelegramId = 111, Username = "admin1", FirstName = "Admin", LastName = "One", IsAdmin = true },
            new() { Id = 2, TelegramId = 222, Username = "admin2", FirstName = "Admin", LastName = "Two", IsAdmin = true }
        };

        TypeAdapterConfig.GlobalSettings.Scan(typeof(TelegramBotUserMapping).Assembly);

        // Act
        var response = admins.Adapt<GetAdminsResponse>();

        // Assert
        response.Should().NotBeNull();
        response.TelegramBotAdmins.Should().HaveCount(2);
        response.TelegramBotAdmins[0].Username.Should().Be("admin1");
        response.TelegramBotAdmins[1].Username.Should().Be("admin2");
    }
}
