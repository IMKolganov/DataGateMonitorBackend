using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.Api.Auth.Registers;
using DataGateMonitor.Services.Api.Auth.Users;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth;

public class UserRegistrationServiceTests
{
    private readonly Mock<IPasswordHasher<User>> _passwordHasher;
    private readonly Mock<ICommandService<UserCredential, int>> _credentialCommand;
    private readonly Mock<ICommandService<UserIdentityLink, int>> _identityLinkCommand;
    private readonly Mock<IUserCredentialQueryService> _credentialQuery;
    private readonly Mock<IUserIdentityLinkQueryService> _identityLinkQuery;
    private readonly Mock<IUserQueryService> _userQuery;
    private readonly Mock<IUserAccountService> _userAccountService;
    private readonly Mock<IEmailConfirmationService> _emailConfirmationService;
    private readonly Mock<ISettingsService> _settingsService;
    private readonly Mock<IAppNotificationFacade> _appNotificationFacade;
    private readonly Mock<ILogger<UserRegistrationService>> _logger;
    private readonly UserRegistrationService _sut;

    public UserRegistrationServiceTests()
    {
        _passwordHasher = new Mock<IPasswordHasher<User>>();
        _credentialCommand = new Mock<ICommandService<UserCredential, int>>();
        _identityLinkCommand = new Mock<ICommandService<UserIdentityLink, int>>();
        _credentialQuery = new Mock<IUserCredentialQueryService>();
        _identityLinkQuery = new Mock<IUserIdentityLinkQueryService>();
        _userQuery = new Mock<IUserQueryService>();
        _userAccountService = new Mock<IUserAccountService>();
        _emailConfirmationService = new Mock<IEmailConfirmationService>();
        _settingsService = new Mock<ISettingsService>();
        _appNotificationFacade = new Mock<IAppNotificationFacade>();
        _logger = new Mock<ILogger<UserRegistrationService>>();
        _identityLinkQuery
            .Setup(q => q.AnyByUserId(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _identityLinkCommand
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityLink link, bool _, CancellationToken _) => link);

        _sut = new UserRegistrationService(
            _passwordHasher.Object,
            _credentialCommand.Object,
            _identityLinkCommand.Object,
            _credentialQuery.Object,
            _identityLinkQuery.Object,
            _userQuery.Object,
            _userAccountService.Object,
            _emailConfirmationService.Object,
            _settingsService.Object,
            _appNotificationFacade.Object,
            _logger.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndCredentialAndReturnsResponse()
    {
        var request = new RegisterUserRequest
        {
            DisplayName = "Test User",
            Email = "test@example.com",
            Login = "test-login",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };
        var normalizedLogin = request.Login!.ToUpperInvariant();

        _credentialQuery.Setup(q => q.GetByNormalizedLogin(normalizedLogin, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCredential?)null);
        _userQuery.Setup(q => q.AnyByEmail(request.Email!, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsService
            .Setup(s => s.GetValueAsync<string>($"{AuthEmailSettingsKeys.RequireEmailConfirmationOnRegister}_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bool");
        _settingsService
            .Setup(s => s.GetValueAsync<bool>(AuthEmailSettingsKeys.RequireEmailConfirmationOnRegister, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        User? capturedUser = null;
        _userAccountService
            .Setup(s => s.CreateUserWithDefaultRoleAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken _) =>
            {
                u.Id = 42;
                return u;
            });

        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), request.Password)).Returns("HASHED_PASSWORD");

        UserCredential? capturedCredential = null;
        _credentialCommand
            .Setup(c => c.Add(It.IsAny<UserCredential>(), true, It.IsAny<CancellationToken>()))
            .Callback<UserCredential, bool, CancellationToken>((c, _, _) => capturedCredential = c)
            .ReturnsAsync((UserCredential c, bool _, CancellationToken _) => c);

        UserIdentityLink? capturedIdentityLink = null;
        _identityLinkCommand
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, It.IsAny<CancellationToken>()))
            .Callback<UserIdentityLink, bool, CancellationToken>((l, _, _) => capturedIdentityLink = l)
            .ReturnsAsync((UserIdentityLink l, bool _, CancellationToken _) => l);
        _emailConfirmationService
            .Setup(s => s.SendConfirmationAsync(42, "test@example.com", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(42, result.UserId);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("test@example.com", result.Email);
        Assert.True(result.HasDashboardAccess);

        Assert.NotNull(capturedUser);
        Assert.Equal("Test User", capturedUser!.DisplayName);
        Assert.Equal("test@example.com", capturedUser.Email);
        Assert.False(capturedUser.IsAdmin);
        Assert.True(capturedUser.HasDashboardAccess);

        Assert.NotNull(capturedCredential);
        Assert.Equal(42, capturedCredential!.UserId);
        Assert.Equal("test-login", capturedCredential.Login);
        Assert.Equal(normalizedLogin, capturedCredential.NormalizedLogin);
        Assert.Equal("HASHED_PASSWORD", capturedCredential.PasswordHash);
        Assert.Equal("AspNetCoreV3", capturedCredential.PasswordAlgo);

        Assert.NotNull(capturedIdentityLink);
        Assert.Equal(42, capturedIdentityLink!.UserId);
        Assert.Equal(AuthIdentityProviders.Local, capturedIdentityLink.Provider);
        Assert.Equal("local:42", capturedIdentityLink.ExternalId);

        _emailConfirmationService.Verify(
            s => s.SendConfirmationAsync(42, "test@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailConfirmationSettingDisabled_SkipsSendingAndMarksConfirmed()
    {
        var request = new RegisterUserRequest
        {
            DisplayName = "Test User",
            Email = "test@example.com",
            Login = "test-login",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };

        _credentialQuery.Setup(q => q.GetByNormalizedLogin("TEST-LOGIN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCredential?)null);
        _userQuery.Setup(q => q.AnyByEmail(request.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _settingsService
            .Setup(s => s.GetValueAsync<string>($"{AuthEmailSettingsKeys.RequireEmailConfirmationOnRegister}_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bool");
        _settingsService
            .Setup(s => s.GetValueAsync<bool>(AuthEmailSettingsKeys.RequireEmailConfirmationOnRegister, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), request.Password)).Returns("HASHED_PASSWORD");

        _userAccountService
            .Setup(s => s.CreateUserWithDefaultRoleAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) =>
            {
                u.Id = 42;
                return u;
            });

        _credentialCommand
            .Setup(c => c.Add(It.IsAny<UserCredential>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCredential c, bool _, CancellationToken _) => c);

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(42, result.UserId);
        _userAccountService.Verify(
            s => s.CreateUserWithDefaultRoleAsync(
                It.Is<User>(u => u.IsEmailConfirmed),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailConfirmationService.Verify(
            s => s.SendConfirmationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenLoginAlreadyExists_ThrowsInvalidOperationException()
    {
        var request = new RegisterUserRequest
        {
            DisplayName = "Test",
            Email = "a@b.com",
            Login = "existing",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };
        _credentialQuery.Setup(q => q.GetByNormalizedLogin("EXISTING", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserCredential { NormalizedLogin = "EXISTING" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(request, CancellationToken.None));

        Assert.Equal("Login is already in use.", ex.Message);
        _userAccountService.Verify(
            s => s.CreateUserWithDefaultRoleAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        var request = new RegisterUserRequest
        {
            DisplayName = "Test",
            Email = "taken@example.com",
            Login = "new-login",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };
        _credentialQuery.Setup(q => q.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCredential?)null);
        _userQuery.Setup(q => q.AnyByEmail("taken@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(request, CancellationToken.None));

        Assert.Equal("Email is already in use.", ex.Message);
        _userAccountService.Verify(
            s => s.CreateUserWithDefaultRoleAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordsDoNotMatch_ThrowsArgumentException()
    {
        var request = new RegisterUserRequest
        {
            DisplayName = "Test",
            Email = "a@b.com",
            Login = "login",
            Password = "StrongPass123!",
            ConfirmPassword = "OtherPass456!"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.RegisterAsync(request, CancellationToken.None));

        Assert.Equal("Passwords do not match.", ex.Message);
        _credentialQuery.Verify(q => q.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "login", "pass1234", "Display name is required.")]
    [InlineData("   ", "login", "pass1234", "Display name is required.")]
    [InlineData("User", null, "pass1234", "Login is required.")]
    [InlineData("User", "   ", "pass1234", "Login is required.")]
    [InlineData("User", "login", null, "Password is required.")]
    [InlineData("User", "login", "   ", "Password is required.")]
    [InlineData("User", "login", "short", "Password must be at least 8 characters long.")]
    public async Task RegisterAsync_InvalidBasicFields_ThrowsArgumentException(
        string? displayName, string? login, string? password, string expectedMessage)
    {
        var request = new RegisterUserRequest
        {
            DisplayName = displayName!,
            Email = "test@example.com",
            Login = login!,
            Password = password!,
            ConfirmPassword = password!
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.RegisterAsync(request, CancellationToken.None));

        Assert.Equal(expectedMessage, ex.Message);
        _credentialQuery.Verify(q => q.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
