using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Services.Users.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses.Dto;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock = new(MockBehavior.Strict);
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _controller = new UserController(_userServiceMock.Object);
    }

    [Fact]
    public async Task RegisterUser_ReturnsOk_WithUserResponse()
    {
        // Arrange
        var req = new RegisterUserFromTgBotRequest
        {
            TelegramId = 12345,
            Username = "tester",
            FirstName = "Test",
            LastName = "User",
            LanguageCode = "en",
            IsPremium = true
        };

        var expected = new UsersResponse
        {
            User = new UserDto
            {
                Id = 10,
                DisplayName = "tester",
                Email = null,
                IsAdmin = false,
                IsBlocked = false,
                HasDashboardAccess = false
            }
        };

        _userServiceMock
            .Setup(s => s.RegisterUserFromTgBot(It.Is<RegisterUserFromTgBotRequest>(r => r.TelegramId == 12345), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.RegisterUser(req, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UsersResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(10, response.Data!.User.Id);
        Assert.Equal("tester", response.Data.User.DisplayName);

        _userServiceMock.VerifyAll();
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOk_WithUsersList()
    {
        // Arrange
        var expected = new GetAllUsersResponse
        {
            Users =
            [
                new UserDto { Id = 1, DisplayName = "u1" },
                new UserDto { Id = 2, DisplayName = "u2" }
            ]
        };

        _userServiceMock
            .Setup(s => s.GetAllUsers(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetAllUsers(CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetAllUsersResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal(2, response.Data!.Users.Count);
        Assert.Equal("u1", response.Data.Users[0].DisplayName);

        _userServiceMock.VerifyAll();
    }

    [Fact]
    public async Task GetUserById_ReturnsOk_WithUser()
    {
        // Arrange
        var req = new GetUserByIdRequest { Id = 42 };

        var expected = new UsersResponse
        {
            User = new UserDto { Id = 42, DisplayName = "answer" }
        };

        _userServiceMock
            .Setup(s => s.GetUserById(It.Is<GetUserByIdRequest>(r => r.Id == 42), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetUserById(req, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UsersResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal(42, response.Data!.User.Id);
        Assert.Equal("answer", response.Data.User.DisplayName);

        _userServiceMock.VerifyAll();
    }

    [Fact]
    public async Task GetUserByExternalId_ReturnsOk_WithUser()
    {
        // Arrange
        var req = new GetUserByExternalIdRequest { ExternalId = "123" };

        var expected = new UsersResponse
        {
            User = new UserDto { Id = 7, DisplayName = "external" }
        };

        _userServiceMock
            .Setup(s => s.GetUserByExternalId(It.Is<GetUserByExternalIdRequest>(r => r.ExternalId == "123"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetUserByExternalId(req, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UsersResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal(7, response.Data!.User.Id);
        Assert.Equal("external", response.Data.User.DisplayName);

        _userServiceMock.VerifyAll();
    }
}
