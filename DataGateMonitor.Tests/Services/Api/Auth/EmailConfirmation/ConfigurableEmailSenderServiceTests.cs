using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.EmailConfirmation;

public class ConfigurableEmailSenderServiceTests
{
    [Fact]
    public async Task SendAsync_WhenProviderIsSmtp_UsesSmtpService()
    {
        var smtp = new SmtpEmailSenderService(
            Options.Create(new EmailSenderSettings()),
            Mock.Of<ILogger<SmtpEmailSenderService>>());
        var resend = new ResendEmailSenderService(
            Options.Create(new EmailSenderSettings { Provider = "resend" }),
            Mock.Of<ILogger<ResendEmailSenderService>>());

        var sut = new ConfigurableEmailSenderService(
            Options.Create(new EmailSenderSettings { Provider = "smtp" }),
            smtp,
            resend);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendAsync("a@b.com", "subj", "body", CancellationToken.None));

        Assert.Contains("Email sender settings are not configured", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenProviderIsResend_UsesResendService()
    {
        var smtp = new SmtpEmailSenderService(
            Options.Create(new EmailSenderSettings { Host = "smtp", FromEmail = "from@test.com", Username = "u", Password = "p" }),
            Mock.Of<ILogger<SmtpEmailSenderService>>());
        var resend = new ResendEmailSenderService(
            Options.Create(new EmailSenderSettings()),
            Mock.Of<ILogger<ResendEmailSenderService>>());

        var sut = new ConfigurableEmailSenderService(
            Options.Create(new EmailSenderSettings { Provider = "resend" }),
            smtp,
            resend);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendAsync("a@b.com", "subj", "body", CancellationToken.None));

        Assert.Contains("Resend settings are not configured", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenProviderUnknown_ThrowsInvalidOperationException()
    {
        var smtp = new SmtpEmailSenderService(
            Options.Create(new EmailSenderSettings()),
            Mock.Of<ILogger<SmtpEmailSenderService>>());
        var resend = new ResendEmailSenderService(
            Options.Create(new EmailSenderSettings()),
            Mock.Of<ILogger<ResendEmailSenderService>>());

        var sut = new ConfigurableEmailSenderService(
            Options.Create(new EmailSenderSettings { Provider = "unknown" }),
            smtp,
            resend);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendAsync("a@b.com", "subj", "body", CancellationToken.None));

        Assert.Contains("Unsupported email provider", ex.Message);
    }
}
