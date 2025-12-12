using OpenVPNGateMonitor.Controllers;

namespace OpenVPNGateMonitor.Services.Api.Auth.Login;

public interface IUserLoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}