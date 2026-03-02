using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.ForgotPassword;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Api.Auth.ForgotPassword;

public class AdminForgotPasswordServiceTests
{
    [Fact]
    public async Task RequestResetCodeAsync_When_LoginOrEmailEmpty_Returns_SameMessageForAll()
    {
        var credentialQuery = new Mock<IUserCredentialQueryService>();
        var userQuery = new Mock<IUserQueryService>();
        var userRoleQuery = new Mock<IUserRoleQueryService>();
        var credentialCommand = new Mock<ICommandService<UserCredential, int>>();
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<AdminForgotPasswordService>>();

        var sut = new AdminForgotPasswordService(
            credentialQuery.Object,
            userQuery.Object,
            userRoleQuery.Object,
            credentialCommand.Object,
            passwordHasher.Object,
            cache,
            logger.Object);

        var request = new AdminForgotPasswordRequest { LoginOrEmail = "   " };
        var result = await sut.RequestResetCodeAsync(request, "127.0.0.1", CancellationToken.None);

        Assert.Equal(AdminForgotPasswordService.SameMessageForAll, result.Message);
        credentialQuery.Verify(c => c.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
