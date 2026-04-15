using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.ForgotPassword;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.ForgotPassword;

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
