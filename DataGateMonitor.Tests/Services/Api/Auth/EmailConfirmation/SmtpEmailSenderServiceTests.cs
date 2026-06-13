using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.EmailConfirmation;

public class SmtpEmailSenderServiceTests
{
    [Fact]
    public async Task SendAsync_WhenRecipientEmpty_ThrowsArgumentException()
    {
        var sut = new SmtpEmailSenderService(
            Options.Create(new EmailSenderSettings
            {
                Host = "smtp.test",
                FromEmail = "from@test.com",
                Username = "user",
                Password = "pass",
            }),
            Mock.Of<ILogger<SmtpEmailSenderService>>());

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.SendAsync("  ", "subject", "body", CancellationToken.None));

        Assert.Equal("toEmail", ex.ParamName);
    }

    [Fact]
    public async Task SendAsync_WhenSettingsMissing_ThrowsInvalidOperationException()
    {
        var sut = new SmtpEmailSenderService(
            Options.Create(new EmailSenderSettings()),
            Mock.Of<ILogger<SmtpEmailSenderService>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendAsync("user@test.com", "subject", "body", CancellationToken.None));

        Assert.Contains("Email sender settings are not configured", ex.Message);
    }
}
