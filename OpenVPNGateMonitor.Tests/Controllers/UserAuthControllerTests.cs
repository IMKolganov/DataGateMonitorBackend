using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Auth.Models;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class UserAuthControllerTests
{
    private readonly Mock<IUserAuthService> _userAuth = new();
    private readonly Mock<IUserQueryService> _users = new();
    private readonly Mock<IAuthenticationService> _authService = new();

    private readonly UserAuthController _controller;

    public UserAuthControllerTests()
    {
        _controller = new UserAuthController(_userAuth.Object, _users.Object);

        // Build service provider with mocked IAuthenticationService
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(_authService.Object);
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = sp
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Login_WhenVerifyFails_Returns_Unauthorized()
    {
        // Arrange
        _userAuth
            .Setup(a => a.VerifyAsync("user", "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserAuthResult.Fail("invalid_credentials"));

        var req = new UserAuthController.LoginRequest("user", "bad");

        // Act
        var result = await _controller.Login(req, CancellationToken.None);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var value = unauthorized.Value!;
        var messageProp = value.GetType().GetProperty("message");
        Assert.NotNull(messageProp);
        Assert.Equal("invalid_credentials", messageProp!.GetValue(value));

        _authService.Verify(
            a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_WhenUserIsBlocked_Returns_Forbid()
    {
        // Arrange
        _userAuth
            .Setup(a => a.VerifyAsync("user", "pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserAuthResult.Success(1));

        _users
            .Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 1,
                DisplayName = "Blocked user",
                IsAdmin = false,
                IsBlocked = true
            });

        var req = new UserAuthController.LoginRequest("user", "pwd");

        // Act
        var result = await _controller.Login(req, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);

        _authService.Verify(
            a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_WhenSuccess_SignsIn_And_Returns_LoginResponse()
    {
        // Arrange
        _userAuth
            .Setup(a => a.VerifyAsync("user", "pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserAuthResult.Success(1));

        _users
            .Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 1,
                DisplayName = "Good user",
                IsAdmin = true,
                IsBlocked = false
            });

        _authService
            .Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                "UserCookie",
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var req = new UserAuthController.LoginRequest("user", "pwd");

        // Act
        var result = await _controller.Login(req, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserAuthController.LoginResponse>(ok.Value);

        Assert.Equal("Good user", response.DisplayName);
        Assert.True(response.IsAdmin);

        _authService.Verify(
            a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                "UserCookie",
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_SignsOut_And_Returns_Ok()
    {
        // Arrange
        _authService
            .Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                "UserCookie",
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await _controller.Logout();

        // Assert
        Assert.IsType<OkResult>(result);

        _authService.Verify(
            a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                "UserCookie",
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }
}
