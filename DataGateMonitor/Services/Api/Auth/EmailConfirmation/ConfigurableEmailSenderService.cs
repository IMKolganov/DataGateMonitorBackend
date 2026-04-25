using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public sealed class ConfigurableEmailSenderService(
    IOptions<EmailSenderSettings> options,
    SmtpEmailSenderService smtpEmailSenderService,
    ResendEmailSenderService resendEmailSenderService) : IEmailSenderService
{
    private readonly EmailSenderSettings _settings = options.Value;

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        var provider = (_settings.Provider ?? "smtp").Trim().ToLowerInvariant();
        return provider switch
        {
            "smtp" => smtpEmailSenderService.SendAsync(toEmail, subject, body, ct),
            "resend" => resendEmailSenderService.SendAsync(toEmail, subject, body, ct),
            _ => throw new InvalidOperationException(
                $"Unsupported email provider '{_settings.Provider}'. Allowed values: smtp, resend.")
        };
    }
}
