namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public interface IEmailSenderService
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
}
