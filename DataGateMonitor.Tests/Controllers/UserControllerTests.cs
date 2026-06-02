using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock = new(MockBehavior.Strict);
    private readonly Mock<IUserMergeService> _userMergeServiceMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new(MockBehavior.Strict);
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _controller = new UserController(
            _userServiceMock.Object,
            _userMergeServiceMock.Object,
            _currentUserServiceMock.Object);
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
    public async Task GetAllUsers_ReturnsOk_WithPagedUsersList()
    {
        // Arrange
        var expected = new GetAllUsersResponse
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 2,
            Users =
            [
                new UserDto { Id = 1, DisplayName = "u1" },
                new UserDto { Id = 2, DisplayName = "u2" }
            ]
        };

        _userServiceMock
            .Setup(s => s.GetUsersPage(It.IsAny<GetAllUsersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetAllUsers(new GetAllUsersRequest(), CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetAllUsersResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal(1, response.Data!.Page);
        Assert.Equal(20, response.Data.PageSize);
        Assert.Equal(2, response.Data.TotalCount);
        Assert.Equal(2, response.Data.Users.Count);
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
