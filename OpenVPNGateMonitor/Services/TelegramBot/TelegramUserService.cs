using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class TelegramUserService(ILogger<TelegramUserService> logger,
    ITelegramBotUserQueryService telegramBotUserQueryService) : ITelegramUserService
{
    public async Task<TelegramBotUser> RegisterUserAsync(TelegramBotUser telegramBotUserRequest, 
        CancellationToken ct)
    {
        var telegramUserRepository = unitOfWork.GetRepository<TelegramBotUser>();
        var telegramBotUser = await telegramBotUserQueryService.GetByTelegramIdAsync(
            telegramBotUserRequest.TelegramId, ct);

        if (telegramBotUser == null)
        {
            var user = telegramBotUserRequest.Adapt<TelegramBotUser>();

            await telegramUserRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation($"User {telegramBotUserRequest.Username} registered");
        }
        
        telegramBotUser = await telegramBotUserQueryService.GetByTelegramIdAsync(
            telegramBotUserRequest.TelegramId, ct);

        return telegramBotUser ?? 
               throw new InvalidOperationException($"Something went wrong when " + 
                                                   $"try to add new " +
                                                   $"TelegramBotUser: {telegramBotUserRequest.TelegramId}");
    }

    public Task<TelegramBotUser> DeleteUserAsync(TelegramBotUser telegramBotUserRequest,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<List<TelegramBotUser>?> GetAdminsAsync(CancellationToken ct)
    {
        var telegramBotUser = await telegramBotUserQueryService.GetAllAdminsAsync(ct);
        
        if (telegramBotUser is { Count: 0 })
        {
            logger.LogError("Admins for telegram bot not found, returning empty list");
            return new List<TelegramBotUser>();
        }
        
        return telegramBotUser;
    }
    
    public async Task<List<TelegramBotUser>?> GetAllUsersAsync(CancellationToken ct)
    {
        var telegramBotUser = await telegramBotUserQueryService.GetAllAsync(ct);
        return telegramBotUser.OrderBy(x=>x.Id).ToList();
    }
    
    public async Task<TelegramBotUser?> GetUserByTelegramIdAsync(long telegramId, CancellationToken ct)
    {
        var user = await telegramBotUserQueryService.GetByTelegramIdAsync(telegramId, ct);

        return user;
    }
    
    public async Task<bool> BlockUserAsync(long telegramId, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await telegramBotUserQueryService.GetByTelegramIdAsync(telegramId, ct);
        
        if (user == null)
        {
            logger.LogWarning($"Attempted to block non-existent user with TelegramId: {telegramId}");
            return false;
        }

        if (user.IsBlocked)
        {
            logger.LogInformation($"User {telegramId} is already blocked.");
            return true;
        }

        user.IsBlocked = true;
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation($"User {telegramId} has been blocked.");
        return true;
    }

    public async Task<bool> UnblockUserAsync(long telegramId, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await telegramBotUserQueryService.GetByTelegramIdAsync(telegramId, ct);
        
        if (user == null)
        {
            logger.LogWarning($"Attempted to unblock non-existent user with TelegramId: {telegramId}");
            return false;
        }

        if (!user.IsBlocked)
        {
            logger.LogInformation($"User {telegramId} is not blocked.");
            return true;
        }

        user.IsBlocked = false;
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation($"User {telegramId} has been unblocked.");
        return true;
    }
    
    public async Task<bool> SetAdminAsync(long telegramId, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await telegramBotUserQueryService.GetByTelegramIdAsync(telegramId, ct);

        if (user == null)
        {
            logger.LogWarning($"Attempted to set admin for non-existent user with TelegramId: {telegramId}");
            return false;
        }

        if (user.IsAdmin)
        {
            logger.LogInformation($"User {telegramId} is already admin.");
            return true;
        }

        user.IsAdmin = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation($"User {telegramId} has been set as admin.");
        return true;
    }

    public async Task<bool> UnsetAdminAsync(long telegramId, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await telegramBotUserQueryService.GetByTelegramIdAsync(telegramId, ct);

        if (user == null)
        {
            logger.LogWarning($"Attempted to unset admin for non-existent user with TelegramId: {telegramId}");
            return false;
        }

        if (!user.IsAdmin)
        {
            logger.LogInformation($"User {telegramId} is not an admin.");
            return true;
        }

        user.IsAdmin = false;
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation($"Admin rights removed from user {telegramId}.");
        return true;
    }
}