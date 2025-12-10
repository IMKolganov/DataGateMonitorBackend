using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

public interface IUserRegistrationService
{
    Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken ct);
}