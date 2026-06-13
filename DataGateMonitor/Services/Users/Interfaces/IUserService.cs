using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Returns dashboard user for the given Telegram user, creating User and identity link if needed.
    /// Requires TelegramBotUser to exist (bot should have registered the user when they requested the code).
    /// </summary>
    Task<User?> GetOrCreateDashboardUserForTelegramAsync(long telegramId, CancellationToken cancellationToken);

    Task<UsersResponse> RegisterUserFromTgBot(RegisterUserFromTgBotRequest request,
        CancellationToken cancellationToken);
    Task<GetAllUsersResponse> GetUsersPage(GetAllUsersRequest request, CancellationToken cancellationToken);
    Task<UsersResponse> GetUserById(GetUserByIdRequest request, CancellationToken cancellationToken);
    Task<UsersResponse> GetUserByExternalId(GetUserByExternalIdRequest request, CancellationToken cancellationToken);
    Task<GetUserEmailConfirmationStatusResponse> GetEmailConfirmationStatus(
        GetUserEmailConfirmationStatusRequest request,
        CancellationToken cancellationToken);
    Task<ConfirmUserEmailResponse> ConfirmEmailManually(ConfirmUserEmailRequest request, CancellationToken cancellationToken);
}