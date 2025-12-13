using OpenVPNGateMonitor.SharedModels.Auth.Google;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;


public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct);
}
