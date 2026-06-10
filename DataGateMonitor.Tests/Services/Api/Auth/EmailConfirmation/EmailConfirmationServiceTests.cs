using Microsoft.Extensions.Caching.Memory;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.AdminEmail;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.EmailTemplates;
using DataGateMonitor.Services.Others;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.EmailConfirmation;

public class EmailConfirmationServiceTests
{
    [Fact]
    public async Task SendConfirmationAsync_WhenEmailEmpty_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.SendConfirmationAsync(1, "  ", CancellationToken.None));

        Assert.Equal("email", ex.ParamName);
    }

    [Fact]
    public async Task SendConfirmationAsync_SendsEmailAndLogsSuccess()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var emailSender = new Mock<IEmailSenderService>();
        emailSender
            .Setup(s => s.SendAsync("user@example.com", "Confirm", "Body", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sentEmailLog = new Mock<ISentEmailLogService>();
        sentEmailLog
            .Setup(l => l.LogAsync(1, "user@example.com", "Confirm", "Body", true, null, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var systemTransactionalEmail = new Mock<ISystemTransactionalEmailService>();
        systemTransactionalEmail
            .Setup(s => s.GetEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Confirm", "Body"));

        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetValueAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var sut = CreateSut(cache, emailSender, sentEmailLog, systemTransactionalEmail, settings);

        await sut.SendConfirmationAsync(1, "user@example.com", CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync("user@example.com", "Confirm", "Body", It.IsAny<CancellationToken>()),
            Times.Once);
        sentEmailLog.Verify(
            l => l.LogAsync(1, "user@example.com", "Confirm", "Body", true, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_WhenCodeMissing_ReturnsFailure()
    {
        var sut = CreateSut();

        var result = await sut.ConfirmAsync("user@example.com", "  ", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Email and code are required.", result.Message);
    }

    [Fact]
    public async Task ConfirmAsync_WhenCodeNotInCache_ReturnsFailure()
    {
        var sut = CreateSut(new MemoryCache(new MemoryCacheOptions()));

        var result = await sut.ConfirmAsync("user@example.com", "123456", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid or expired confirmation code.", result.Message);
    }

    private static EmailConfirmationService CreateSut(
        IMemoryCache? cache = null,
        Mock<IEmailSenderService>? emailSender = null,
        Mock<ISentEmailLogService>? sentEmailLog = null,
        Mock<ISystemTransactionalEmailService>? systemTransactionalEmail = null,
        Mock<ISettingsService>? settings = null)
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        emailSender ??= new Mock<IEmailSenderService>();
        sentEmailLog ??= new Mock<ISentEmailLogService>();
        systemTransactionalEmail ??= new Mock<ISystemTransactionalEmailService>();
        settings ??= new Mock<ISettingsService>();

        return new EmailConfirmationService(
            cache,
            new Mock<IUserQueryService>().Object,
            new Mock<ICommandService<User, int>>().Object,
            emailSender.Object,
            settings.Object,
            sentEmailLog.Object,
            systemTransactionalEmail.Object);
    }
}
