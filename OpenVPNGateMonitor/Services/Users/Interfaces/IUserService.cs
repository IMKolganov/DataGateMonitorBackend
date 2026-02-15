using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;

namespace OpenVPNGateMonitor.Services.Users.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Returns dashboard user for the given Telegram user, creating User and identity link if needed.
    /// Requires TelegramBotUser to exist (bot should have registered the user when they requested the code).
    /// </summary>
    Task<User?> GetOrCreateDashboardUserForTelegramAsync(long telegramId, CancellationToken cancellationToken);

    Task<UsersResponse> RegisterUserFromTgBot(RegisterUserFromTgBotRequest request,
        CancellationToken cancellationToken);
    Task<GetAllUsersResponse> GetAllUsers(CancellationToken cancellationToken);
    Task<UsersResponse> GetUserById(GetUserByIdRequest request, CancellationToken cancellationToken);
    Task<UsersResponse> GetUserByExternalId(GetUserByExternalIdRequest request, CancellationToken cancellationToken);
}