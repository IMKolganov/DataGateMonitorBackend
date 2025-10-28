using FluentAssertions;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Mapping.TelegramBotUser.Mappings;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

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
        var result = await _controller.GetAdmins(CancellationToken.None);

        // Assert
        var okResult = result.Result as OkObjectResult;
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