using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.AdminEmail;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.Api.Auth.ForgotPassword;
using DataGateMonitor.Services.EmailTemplates;
using DataGateMonitor.Services.Others.Notifications;
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
        var credentialCommand = new Mock<ICommandService<UserCredential, int>>();
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var emailSender = new Mock<IEmailSenderService>();
        var sentEmailLog = new Mock<ISentEmailLogService>();
        var systemTransactionalEmail = new Mock<ISystemTransactionalEmailService>();
        var appNotifications = new Mock<IAppNotificationFacade>();
        var logger = new Mock<ILogger<AdminForgotPasswordService>>();

        var sut = new AdminForgotPasswordService(
            credentialQuery.Object,
            userQuery.Object,
            credentialCommand.Object,
            passwordHasher.Object,
            cache,
            emailSender.Object,
            sentEmailLog.Object,
            systemTransactionalEmail.Object,
            appNotifications.Object,
            logger.Object);

        var request = new AdminForgotPasswordRequest { LoginOrEmail = "   " };
        var result = await sut.RequestResetCodeAsync(request, "127.0.0.1", CancellationToken.None);

        Assert.Equal(AdminForgotPasswordService.SameMessageForAll, result.Message);
        credentialQuery.Verify(c => c.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
