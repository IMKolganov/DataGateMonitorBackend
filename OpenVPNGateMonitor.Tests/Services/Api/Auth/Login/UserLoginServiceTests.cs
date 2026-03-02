using Moq;
using OpenVPNGateMonitor.Services.Api.Auth.Login;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Api.Auth.Login;

public class UserLoginServiceTests
{
    [Fact]
    public async Task LoginAsync_When_LoginEmpty_ThrowsArgumentException()
    {
        var credentialQuery = new Mock<OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable.IUserCredentialQueryService>();
        var userQuery = new Mock<OpenVPNGateMonitor.DataBase.Services.Query.UserTable.IUserQueryService>();
        var passwordHasher = new Mock<Microsoft.AspNetCore.Identity.IPasswordHasher<OpenVPNGateMonitor.Models.User>>();
        var tokenValidator = new Mock<OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces.IGoogleTokenValidator>();
        var userIdentityLinkCommand = new Mock<OpenVPNGateMonitor.DataBase.Services.Command.Interfaces.ICommandService<OpenVPNGateMonitor.Models.UserIdentityLink, int>>();
        var userIdentityLinkQuery = new Mock<OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable.IUserIdentityLinkQueryService>();
        var userAccountService = new Mock<OpenVPNGateMonitor.Services.Api.Auth.Users.IUserAccountService>();
        var tokenService = new Mock<ITokenService>();
        var httpContextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

        var sut = new UserLoginService(
            credentialQuery.Object,
            userQuery.Object,
            passwordHasher.Object,
            tokenValidator.Object,
            userIdentityLinkCommand.Object,
            userIdentityLinkQuery.Object,
            userAccountService.Object,
            tokenService.Object,
            httpContextAccessor.Object);

        var request = new OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests.LoginRequest
        {
            Login = "  ",
            Password = "pass"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.LoginAsync(request, CancellationToken.None));

        Assert.Equal("Login is required.", ex.Message);
        credentialQuery.Verify(c => c.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_When_PasswordEmpty_ThrowsArgumentException()
    {
        var credentialQuery = new Mock<OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable.IUserCredentialQueryService>();
        var userQuery = new Mock<OpenVPNGateMonitor.DataBase.Services.Query.UserTable.IUserQueryService>();
        var passwordHasher = new Mock<Microsoft.AspNetCore.Identity.IPasswordHasher<OpenVPNGateMonitor.Models.User>>();
        var tokenValidator = new Mock<OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces.IGoogleTokenValidator>();
        var userIdentityLinkCommand = new Mock<OpenVPNGateMonitor.DataBase.Services.Command.Interfaces.ICommandService<OpenVPNGateMonitor.Models.UserIdentityLink, int>>();
        var userIdentityLinkQuery = new Mock<OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable.IUserIdentityLinkQueryService>();
        var userAccountService = new Mock<OpenVPNGateMonitor.Services.Api.Auth.Users.IUserAccountService>();
        var tokenService = new Mock<ITokenService>();
        var httpContextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

        var sut = new UserLoginService(
            credentialQuery.Object,
            userQuery.Object,
            passwordHasher.Object,
            tokenValidator.Object,
            userIdentityLinkCommand.Object,
            userIdentityLinkQuery.Object,
            userAccountService.Object,
            tokenService.Object,
            httpContextAccessor.Object);

        var request = new OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests.LoginRequest
        {
            Login = "user",
            Password = ""
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.LoginAsync(request, CancellationToken.None));

        Assert.Equal("Password is required.", ex.Message);
    }
}
