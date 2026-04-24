using FluentAssertions;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Mapping.TelegramBotUser.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

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
        var admins = new List<TelegramBotUser>
        {
            new() { Id = 1, TelegramId = 111, Username = "admin1", FirstName = "Admin", LastName = "One", IsAdmin = true },
            new() { Id = 2, TelegramId = 222, Username = "admin2", FirstName = "Admin", LastName = "Two", IsAdmin = true }
        };

        _telegramUserServiceMock
            .Setup(s => s.GetAdminsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(admins);

        var result = await _controller.GetAdmins(CancellationToken.None);

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
        var admins = new List<TelegramBotUser>
        {
            new() { Id = 1, TelegramId = 111, Username = "admin1", FirstName = "Admin", LastName = "One", IsAdmin = true },
            new() { Id = 2, TelegramId = 222, Username = "admin2", FirstName = "Admin", LastName = "Two", IsAdmin = true }
        };

        TypeAdapterConfig.GlobalSettings.Scan(typeof(TelegramBotUserMapping).Assembly);

        var response = admins.Adapt<GetAdminsResponse>();

        response.Should().NotBeNull();
        response.TelegramBotAdmins.Should().HaveCount(2);
        response.TelegramBotAdmins[0].Username.Should().Be("admin1");
        response.TelegramBotAdmins[1].Username.Should().Be("admin2");
    }

    // ---------- UserExists ----------

    [Fact]
    public async Task UserExists_WhenUserFound_ReturnsTrue()
    {
        var user = new TelegramBotUser { Id = 1, TelegramId = 123 };

        _telegramUserServiceMock
            .Setup(s => s.GetUserByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var req = new TelegramUserActionRequest { TelegramId = 123 };

        var result = await _controller.UserExists(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().BeTrue();

        _telegramUserServiceMock.Verify(
            s => s.GetUserByTelegramIdAsync(123, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserExists_WhenUserNotFound_ReturnsFalse()
    {
        _telegramUserServiceMock
            .Setup(s => s.GetUserByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        var req = new TelegramUserActionRequest { TelegramId = 123 };

        var result = await _controller.UserExists(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().BeFalse();
    }

    // ---------- GetUser ----------

    [Fact]
    public async Task GetUser_ReturnsOk_WithMappedUser()
    {
        var user = new TelegramBotUser
        {
            Id = 10,
            TelegramId = 555,
            Username = "user1",
            FirstName = "Ivan",
            LastName = "Kolganov"
        };

        _telegramUserServiceMock
            .Setup(s => s.GetUserAsync(555, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var req = new UserRequest { TelegramId = 555 };

        var result = await _controller.GetUser(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserResponse>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.TelegramBotUser.TelegramId.Should().Be(555);
        response.Data.TelegramBotUser.Username.Should().Be("user1");
    }

    // ---------- GetAllUsers ----------

    [Fact]
    public async Task GetAllUsers_ReturnsOk_WithUsersList()
    {
        var users = new List<TelegramBotUser>
        {
            new() { Id = 1, TelegramId = 100, Username = "u1" },
            new() { Id = 2, TelegramId = 200, Username = "u2" }
        };

        _telegramUserServiceMock
            .Setup(s => s.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _controller.GetAllUsers(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetAllTelegramUsersResponse>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    // ---------- Block / Unblock / SetAdmin / UnsetAdmin ----------

    [Fact]
    public async Task BlockUser_ReturnsOk_WithServiceResult()
    {
        _telegramUserServiceMock
            .Setup(s => s.BlockUserAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var req = new TelegramUserActionRequest { TelegramId = 123 };

        var result = await _controller.BlockUser(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().BeTrue();

        _telegramUserServiceMock.Verify(
            s => s.BlockUserAsync(123, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnblockUser_ReturnsOk_WithServiceResult()
    {
        _telegramUserServiceMock
            .Setup(s => s.UnblockUserAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var req = new TelegramUserActionRequest { TelegramId = 123 };

        var result = await _controller.UnblockUser(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().BeTrue();

        _telegramUserServiceMock.Verify(
            s => s.UnblockUserAsync(123, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAdmin_ReturnsOk_WithServiceResult()
    {
        _telegramUserServiceMock
            .Setup(s => s.SetAdminAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var req = new TelegramUserActionRequest { TelegramId = 123 };

        var result = await _controller.SetAdmin(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().BeTrue();

        _telegramUserServiceMock.Verify(
            s => s.SetAdminAsync(123, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsetAdmin_ReturnsOk_WithServiceResult()
    {
        _telegramUserServiceMock
            .Setup(s => s.UnsetAdminAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var req = new TelegramUserActionRequest { TelegramId = 123 };

        var result = await _controller.UnsetAdmin(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);

        response.Success.Should().BeTrue();
        response.Data.Should().BeTrue();

        _telegramUserServiceMock.Verify(
            s => s.UnsetAdminAsync(123, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
