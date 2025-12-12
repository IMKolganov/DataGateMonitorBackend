using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Login;

public interface IUserLoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}