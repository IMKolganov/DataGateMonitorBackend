using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.Auth.Totp;
using DataGateMonitor.Services.Api.Auth.Users;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Login;

public class UserLoginServicePasswordTests
{
    private static void SetupTotpPassthrough(Mock<IAdminTotpService> adminTotpService)
    {
        adminTotpService
            .Setup(s => s.ApplyAdminTotpGateAsync(
                It.IsAny<User>(),
                It.IsAny<UserCredential?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Func<CancellationToken, Task<LoginResponse>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((User _, UserCredential? _, string? _, string? _, string? _, Func<CancellationToken, Task<LoginResponse>> issue, CancellationToken ct) =>
                issue(ct));
    }

    [Fact]
    public async Task LoginAsync_WhenNoIdentityLink_CreatesLocalLinkBeforeIssuingToken()
    {
        var credentialQuery = new Mock<IUserCredentialQueryService>();
        var userQuery = new Mock<IUserQueryService>();
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var tokenValidator = new Mock<IGoogleTokenValidator>();
        var userIdentityLinkCommand = new Mock<ICommandService<UserIdentityLink, int>>();
        var userCommand = new Mock<ICommandService<User, int>>();
        var userIdentityLinkQuery = new Mock<IUserIdentityLinkQueryService>();
        var userAccountService = new Mock<IUserAccountService>();
        var tokenService = new Mock<ITokenService>();
        var adminTotpService = new Mock<IAdminTotpService>();
        var appNotificationFacade = new Mock<IAppNotificationFacade>();
        var logger = new Mock<ILogger<UserLoginService>>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        SetupTotpPassthrough(adminTotpService);

        var user = new User { Id = 42, DisplayName = "Email User", Email = "u@example.com", IsEmailConfirmed = true };
        var credential = new UserCredential
        {
            UserId = 42,
            Login = "email-user",
            NormalizedLogin = "EMAIL-USER",
            PasswordHash = "HASH",
        };

        credentialQuery
            .Setup(q => q.GetByNormalizedLogin("EMAIL-USER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        userQuery.Setup(q => q.GetById(42, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, "HASH", "Secret123!"))
            .Returns(PasswordVerificationResult.Success);

        userIdentityLinkQuery
            .Setup(q => q.AnyByUserId(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UserIdentityLink? capturedLink = null;
        userIdentityLinkCommand
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, It.IsAny<CancellationToken>()))
            .Callback<UserIdentityLink, bool, CancellationToken>((l, _, _) => capturedLink = l)
            .ReturnsAsync((UserIdentityLink l, bool _, CancellationToken _) => l);

        tokenService
            .Setup(t => t.IssueAsync(42, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair("access", DateTimeOffset.UtcNow.AddHours(1), "refresh", DateTimeOffset.UtcNow.AddDays(1)));

        var sut = new UserLoginService(
            credentialQuery.Object,
            userQuery.Object,
            passwordHasher.Object,
            tokenValidator.Object,
            userIdentityLinkCommand.Object,
            userCommand.Object,
            userIdentityLinkQuery.Object,
            userAccountService.Object,
            tokenService.Object,
            adminTotpService.Object,
            appNotificationFacade.Object,
            logger.Object,
            httpContextAccessor.Object);

        await sut.LoginAsync(new LoginRequest { Login = "email-user", Password = "Secret123!" }, CancellationToken.None);

        Assert.NotNull(capturedLink);
        Assert.Equal(42, capturedLink!.UserId);
        Assert.Equal(AuthIdentityProviders.Local, capturedLink.Provider);
        Assert.Equal("local:42", capturedLink.ExternalId);

        userIdentityLinkCommand.Verify(
            c => c.Add(It.IsAny<UserIdentityLink>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
        tokenService.Verify(
            t => t.IssueAsync(42, null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenTelegramLinkAlreadyExists_DoesNotCreateLocalLink()
    {
        var credentialQuery = new Mock<IUserCredentialQueryService>();
        var userQuery = new Mock<IUserQueryService>();
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var tokenValidator = new Mock<IGoogleTokenValidator>();
        var userIdentityLinkCommand = new Mock<ICommandService<UserIdentityLink, int>>();
        var userCommand = new Mock<ICommandService<User, int>>();
        var userIdentityLinkQuery = new Mock<IUserIdentityLinkQueryService>();
        var userAccountService = new Mock<IUserAccountService>();
        var tokenService = new Mock<ITokenService>();
        var adminTotpService = new Mock<IAdminTotpService>();
        var appNotificationFacade = new Mock<IAppNotificationFacade>();
        var logger = new Mock<ILogger<UserLoginService>>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        SetupTotpPassthrough(adminTotpService);

        var user = new User { Id = 5, DisplayName = "Tg User", IsEmailConfirmed = true };
        var credential = new UserCredential
        {
            UserId = 5,
            NormalizedLogin = "TGUSER",
            PasswordHash = "HASH",
        };

        credentialQuery
            .Setup(q => q.GetByNormalizedLogin("TGUSER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        userQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, "HASH", "Secret123!"))
            .Returns(PasswordVerificationResult.Success);

        userIdentityLinkQuery
            .Setup(q => q.AnyByUserId(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        tokenService
            .Setup(t => t.IssueAsync(5, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair("access", DateTimeOffset.UtcNow.AddHours(1), "refresh", DateTimeOffset.UtcNow.AddDays(1)));

        var sut = new UserLoginService(
            credentialQuery.Object,
            userQuery.Object,
            passwordHasher.Object,
            tokenValidator.Object,
            userIdentityLinkCommand.Object,
            userCommand.Object,
            userIdentityLinkQuery.Object,
            userAccountService.Object,
            tokenService.Object,
            adminTotpService.Object,
            appNotificationFacade.Object,
            logger.Object,
            httpContextAccessor.Object);

        await sut.LoginAsync(new LoginRequest { Login = "tguser", Password = "Secret123!" }, CancellationToken.None);

        userIdentityLinkCommand.Verify(
            c => c.Add(It.IsAny<UserIdentityLink>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        tokenService.Verify(
            t => t.IssueAsync(5, null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
