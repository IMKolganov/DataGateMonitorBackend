using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class TelegramUserService(ILogger<TelegramUserService> logger) : ITelegramUserService
{
    public async Task<TelegramBotUser> RegisterUserAsync(TelegramBotUser telegramBotUserRequest, 
        CancellationToken cancellationToken)
    {
        var telegramUserRepository = unitOfWork.GetRepository<TelegramBotUser>();
        var telegramBotUser = await telegramUserRepository.Query
            .FirstOrDefaultAsync(u => u.TelegramId == telegramBotUserRequest.TelegramId, 
                cancellationToken: cancellationToken);

        if (telegramBotUser == null)
        {
            var user = telegramBotUserRequest.Adapt<TelegramBotUser>();

            await telegramUserRepository.AddAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation($"User {telegramBotUserRequest.Username} registered");
        }
        
        telegramBotUser = await telegramUserRepository.Query
            .FirstOrDefaultAsync(u => u.TelegramId == telegramBotUserRequest.TelegramId, 
                cancellationToken: cancellationToken);

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

    public async Task<List<TelegramBotUser>?> GetAdminsAsync(CancellationToken cancellationToken)
    {
        var telegramBotUser = await unitOfWork.GetQuery<TelegramBotUser>().AsQueryable()
            .Where(u => u.IsAdmin).ToListAsync(cancellationToken: cancellationToken);
        
        if (telegramBotUser is { Count: 0 })
        {
            logger.LogError("Admins for telegram bot not found, returning empty list");
            return new List<TelegramBotUser>();
        }
        
        return telegramBotUser;
    }
    
    public async Task<List<TelegramBotUser>?> GetAllUsersAsync(CancellationToken cancellationToken)
    {
        var telegramBotUser = await unitOfWork.GetQuery<TelegramBotUser>().AsQueryable()
            .OrderBy(x=>x.Id)
            .ToListAsync(cancellationToken: cancellationToken);
        
        return telegramBotUser;
    }
    
    public async Task<TelegramBotUser?> GetUserByTelegramIdAsync(long telegramId, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.GetQuery<TelegramBotUser>().AsQueryable()
            .Where(x => x.TelegramId == telegramId)
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
    
    public async Task<bool> BlockUserAsync(long telegramId, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await repo.Query.FirstOrDefaultAsync(x => x.TelegramId == telegramId, 
            cancellationToken);
        
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
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation($"User {telegramId} has been blocked.");
        return true;
    }

    public async Task<bool> UnblockUserAsync(long telegramId, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await repo.Query.FirstOrDefaultAsync(x => x.TelegramId == telegramId, 
            cancellationToken);
        
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
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation($"User {telegramId} has been unblocked.");
        return true;
    }
    
    public async Task<bool> SetAdminAsync(long telegramId, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await repo.Query.FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken);

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

    public async Task<bool> UnsetAdminAsync(long telegramId, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<TelegramBotUser>();
        var user = await repo.Query.FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken);

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
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation($"Admin rights removed from user {telegramId}.");
        return true;
    }
}