using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public interface IEmailConfirmationService
{
    Task SendConfirmationAsync(int userId, string email, CancellationToken ct);
    Task<ConfirmEmailResponse> ConfirmAsync(string email, string code, CancellationToken ct);
}
