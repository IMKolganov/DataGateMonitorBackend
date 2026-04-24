using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IUserRegistrationService
{
    Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken ct);
}