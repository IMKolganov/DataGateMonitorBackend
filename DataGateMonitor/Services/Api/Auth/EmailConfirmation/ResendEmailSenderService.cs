using Microsoft.Extensions.Options;
using Resend;

namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public sealed class ResendEmailSenderService(
    IOptions<EmailSenderSettings> options,
    ILogger<ResendEmailSenderService> logger) : IEmailSenderService
{
    private readonly EmailSenderSettings _settings = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));

        ValidateSettings();

        var resend = ResendClient.Create(_settings.ResendApiKey);
        var response = await resend.EmailSendAsync(new EmailMessage
        {
            From = _settings.FromEmail,
            To = toEmail,
            Subject = subject,
            HtmlBody = body
        }, ct);

        if (!response.Success)
        {
            logger.LogError("Resend failed to send email to {Email}.", toEmail);
            throw new InvalidOperationException("Resend email delivery failed.");
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.ResendApiKey)
            || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException(
                "Resend settings are not configured. Set EmailSender:ResendApiKey and EmailSender:FromEmail.");
        }
    }
}
