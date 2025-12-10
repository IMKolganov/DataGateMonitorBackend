using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

public interface IGoogleAuthService
{
    Task<GoogleLoginResponse> LoginWithGoogleAsync(string idToken, CancellationToken ct);
}