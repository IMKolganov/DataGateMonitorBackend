using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class TelegramUserService(
    ILogger<TelegramUserService> logger,
    ITelegramBotUserQueryService telegramBotUserQueryService,
    ICommandService<TelegramBotUser, int> telegramBotUserCommandService) : ITelegramUserService
{

    public async Task<TelegramBotUser> RegisterUserAsync(
        TelegramBotUser telegramBotUserRequest,
        CancellationToken ct)
    {
        var telegramBotUser = await telegramBotUserQueryService
            .GetByTelegramId(telegramBotUserRequest.TelegramId, ct);

        var now = DateTimeOffset.UtcNow;

        if (telegramBotUser == null)
        {
            var user = telegramBotUserRequest.Adapt<TelegramBotUser>();
            user.CreateDate = now;
            user.LastUpdate = now;

            await telegramBotUserCommandService.Add(user, saveChanges: true, ct);
            logger.LogInformation("User {TelegramId} registered", telegramBotUserRequest.TelegramId);
            return user;
        }

        telegramBotUserRequest.Adapt(telegramBotUser);
        telegramBotUser.LastUpdate = now;

        await telegramBotUserCommandService.Update(telegramBotUser, saveChanges: true, ct);
        logger.LogInformation("User {TelegramId} updated", telegramBotUserRequest.TelegramId);

        return telegramBotUser;
    }

    public async Task<TelegramBotUser> GetUserAsync(long telegramId, CancellationToken cancellationToken)
    {
        return await telegramBotUserQueryService
            .GetByTelegramId(telegramId, cancellationToken) 
               ?? throw new InvalidOperationException("Telegram user not found");
    }

    public Task<TelegramBotUser> DeleteUserAsync(TelegramBotUser telegramBotUserRequest,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<List<TelegramBotUser>?> GetAdminsAsync(CancellationToken ct)
    {
        var telegramBotUser = await telegramBotUserQueryService.GetAllAdmins(ct);

        if (telegramBotUser is { Count: 0 })
        {
            logger.LogError("Admins for telegram bot not found, returning empty list");
            return new List<TelegramBotUser>();
        }

        return telegramBotUser;
    }

    public async Task<List<TelegramBotUser>?> GetAllUsersAsync(CancellationToken ct)
    {
        var telegramBotUser = await telegramBotUserQueryService.GetAll(ct);
        return telegramBotUser.OrderBy(x => x.Id).ToList();
    }

    public async Task<TelegramBotUser?> GetUserByTelegramIdAsync(long telegramId, CancellationToken ct)
    {
        var user = await telegramBotUserQueryService.GetByTelegramId(telegramId, ct);

        return user;
    }

    public async Task<bool> BlockUserAsync(long telegramId, CancellationToken ct)
    {
        var user = await telegramBotUserQueryService.GetByTelegramId(telegramId, ct);
        if (user == null)
        {
            logger.LogWarning("Attempted to block non-existent user with TelegramId: {TelegramId}", telegramId);
            return false;
        }

        if (user.IsBlocked)
        {
            logger.LogInformation("User {TelegramId} is already blocked.", telegramId);
            return true;
        }

        user.IsBlocked = true;
        user.LastUpdate = DateTimeOffset.UtcNow;
        await telegramBotUserCommandService.Update(user, saveChanges: true, ct);
        logger.LogInformation("User {TelegramId} has been blocked.", telegramId);
        return true;
    }

    public async Task<bool> UnblockUserAsync(long telegramId, CancellationToken ct)
    {
        var user = await telegramBotUserQueryService.GetByTelegramId(telegramId, ct);
        if (user == null)
        {
            logger.LogWarning("Attempted to unblock non-existent user with TelegramId: {TelegramId}", telegramId);
            return false;
        }

        if (!user.IsBlocked)
        {
            logger.LogInformation("User {TelegramId} is not blocked.", telegramId);
            return true;
        }

        user.IsBlocked = false;
        user.LastUpdate = DateTimeOffset.UtcNow;
        await telegramBotUserCommandService.Update(user, saveChanges: true, ct);
        logger.LogInformation("User {TelegramId} has been unblocked.", telegramId);
        return true;
    }

    public async Task<bool> SetAdminAsync(long telegramId, CancellationToken ct)
    {
        var user = await telegramBotUserQueryService.GetByTelegramId(telegramId, ct);
        if (user == null)
        {
            logger.LogWarning("Attempted to set admin for non-existent user with TelegramId: {TelegramId}", telegramId);
            return false;
        }

        if (user.IsAdmin)
        {
            logger.LogInformation("User {TelegramId} is already admin.", telegramId);
            return true;
        }

        user.IsAdmin = true;
        user.LastUpdate = DateTimeOffset.UtcNow;
        await telegramBotUserCommandService.Update(user, saveChanges: true, ct);
        logger.LogInformation("User {TelegramId} has been set as admin.", telegramId);
        return true;
    }

    public async Task<bool> UnsetAdminAsync(long telegramId, CancellationToken ct)
    {
        var user = await telegramBotUserQueryService.GetByTelegramId(telegramId, ct);
        if (user == null)
        {
            logger.LogWarning("Attempted to unset admin for non-existent user with TelegramId: {TelegramId}",
                telegramId);
            return false;
        }

        if (!user.IsAdmin)
        {
            logger.LogInformation("User {TelegramId} is not an admin.", telegramId);
            return true;
        }

        user.IsAdmin = false;
        user.LastUpdate = DateTimeOffset.UtcNow;
        await telegramBotUserCommandService.Update(user, saveChanges: true, ct);
        logger.LogInformation("Admin rights removed from user {TelegramId}.", telegramId);
        return true;
    }
}