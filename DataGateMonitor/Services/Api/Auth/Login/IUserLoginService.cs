using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.Login;

public interface IUserLoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<GoogleLoginResponse> LoginWithGoogleAsync(string idToken, CancellationToken ct);
}