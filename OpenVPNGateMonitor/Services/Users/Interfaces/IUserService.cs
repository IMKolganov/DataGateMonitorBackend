using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;

namespace OpenVPNGateMonitor.Services.Users.Interfaces;

public interface IUserService
{
    Task<UsersResponse> RegisterUserFromTgBot(RegisterUserFromTgBotRequest request, 
        CancellationToken cancellationToken);
    Task<GetAllUsersResponse> GetAllUsers(CancellationToken cancellationToken);
    Task<UsersResponse> GetUserById(GetUserByIdRequest request, CancellationToken cancellationToken);
    Task<UsersResponse> GetUserByExternalId(GetUserByExternalIdRequest request, CancellationToken cancellationToken);
}