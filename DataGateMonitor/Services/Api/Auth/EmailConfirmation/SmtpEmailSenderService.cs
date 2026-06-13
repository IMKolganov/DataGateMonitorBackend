using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public sealed class SmtpEmailSenderService(
    IOptions<EmailSenderSettings> options,
    ILogger<SmtpEmailSenderService> logger) : IEmailSenderService
{
    private readonly EmailSenderSettings _settings = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));

        ValidateSettings();

        using var message = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(_settings.FromName)
                ? new MailAddress(_settings.FromEmail)
                : new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        using var registration = ct.Register(client.SendAsyncCancel);
        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email confirmation message to {Email}", toEmail);
            throw;
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Host)
            || string.IsNullOrWhiteSpace(_settings.FromEmail)
            || string.IsNullOrWhiteSpace(_settings.Username)
            || string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException(
                "Email sender settings are not configured. Configure EmailSender section in configuration.");
        }
    }
}
