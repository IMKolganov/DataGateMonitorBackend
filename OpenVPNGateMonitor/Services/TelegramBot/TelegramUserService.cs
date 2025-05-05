using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class TelegramUserService(ILogger<TelegramUserService> logger, IUnitOfWork unitOfWork) : ITelegramUserService
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
}