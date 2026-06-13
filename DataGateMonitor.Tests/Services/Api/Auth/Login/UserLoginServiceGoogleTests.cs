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
using DataGateMonitor.SharedModels.Auth.Google;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Login;

public class UserLoginServiceGoogleTests
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
    public async Task LoginWithGoogleAsync_WhenNewUser_NotifiesAdmins()
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
        credentialQuery
            .Setup(c => c.GetByUserId(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCredential?)null);

        tokenValidator
            .Setup(v => v.ValidateAsync("google-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo
            {
                Subject = "google-sub-1",
                Email = "new@example.com",
                Name = "New Google User",
            });

        userIdentityLinkQuery
            .Setup(q => q.GetByProviderAndExternalId("google", "google-sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityLink?)null);
        userQuery
            .Setup(q => q.GetByEmail("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var createdUser = new User
        {
            Id = 99,
            DisplayName = "New Google User",
            Email = "new@example.com",
        };

        userAccountService
            .Setup(s => s.CreateUserWithDefaultRoleAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        userIdentityLinkCommand
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityLink link, bool _, CancellationToken _) => link);

        tokenService
            .Setup(t => t.IssueAsync(99, "google-sub-1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair("access", DateTime.UtcNow.AddHours(1), "refresh", DateTime.UtcNow.AddDays(1)));

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

        var response = await sut.LoginWithGoogleAsync("google-token", CancellationToken.None);

        Assert.True(response.IsNewUser);
        appNotificationFacade.Verify(
            f => f.UserRegistered(99, "New Google User", null, "new@example.com", "Google", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WhenExistingLink_DoesNotNotifyAdmins()
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
        credentialQuery
            .Setup(c => c.GetByUserId(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCredential?)null);

        tokenValidator
            .Setup(v => v.ValidateAsync("google-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo { Subject = "google-sub-2", Email = "old@example.com", Name = "Old" });

        userIdentityLinkQuery
            .Setup(q => q.GetByProviderAndExternalId("google", "google-sub-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityLink { UserId = 5 });

        userQuery
            .Setup(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 5, DisplayName = "Old", Email = "old@example.com" });

        tokenService
            .Setup(t => t.IssueAsync(5, "google-sub-2", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPair("access", DateTime.UtcNow.AddHours(1), "refresh", DateTime.UtcNow.AddDays(1)));

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

        var response = await sut.LoginWithGoogleAsync("google-token", CancellationToken.None);

        Assert.False(response.IsNewUser);
        appNotificationFacade.Verify(
            f => f.UserRegistered(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        userIdentityLinkCommand.Verify(
            c => c.Add(
                It.Is<UserIdentityLink>(l => l.Provider == AuthIdentityProviders.Local),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
