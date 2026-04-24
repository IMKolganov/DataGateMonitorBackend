using DataGateMonitor.SharedModels.Auth.Google;

namespace DataGateMonitor.Services.Api.Auth.Registers.Interfaces;


public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct);
}
