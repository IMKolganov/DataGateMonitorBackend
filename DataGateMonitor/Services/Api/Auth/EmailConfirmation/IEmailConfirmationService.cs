using DataGateMonitor.Services.Api.Auth.EmailConfirmation.Models;

namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public interface IEmailConfirmationService
{
    Task SendConfirmationAsync(int userId, string email, CancellationToken ct);
    Task<ConfirmEmailResponse> ConfirmAsync(string email, string code, CancellationToken ct);
}
