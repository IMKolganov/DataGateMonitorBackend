using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Users.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses.Dto;

namespace OpenVPNGateMonitor.Services.Users;

public class UserServices(
    IUserQueryService userQueryService,
    ITelegramBotUserQueryService telegramBotUserQueryService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IQuotaPlanQueryService quotaPlanQueryService,
    ICommandService<User, int> userCommandService,
    ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
    ICommandService<UserQuotaPlan, int> userQuotaPlanCommandService,
    ICommandService<TelegramBotUser, int> telegramBotUserCommandService,
    ILogger<UserServices> logger
) : IUserService
{
    public async Task<UsersResponse> RegisterUserFromTgBot(
        RegisterUserFromTgBotRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // 1) Ensure TelegramBotUser exists (read via query, mutate via command)
        var telegramBotUser = await telegramBotUserQueryService
            .GetByTelegramIdAsync(request.TelegramId, cancellationToken);

        if (telegramBotUser == null)
        {
            telegramBotUser = request.Adapt<TelegramBotUser>();
            telegramBotUser.CreateDate = now;
            telegramBotUser.LastUpdate = now;
            telegramBotUser.IsBlocked = false;
            telegramBotUser.IsAdmin = false;

            telegramBotUser = await telegramBotUserCommandService.AddAsync(telegramBotUser, saveChanges: true,
                cancellationToken);
            logger.LogInformation("Telegram user {TelegramId} created", request.TelegramId);
        }
        else
        {
            request.Adapt(telegramBotUser);
            telegramBotUser.LastUpdate = now;

            await telegramBotUserCommandService.UpdateAsync(telegramBotUser, saveChanges: true, cancellationToken);
            logger.LogInformation("Telegram user {TelegramId} updated", request.TelegramId);
        }

        // 2) Resolve dashboard User via identity link (read via query)
        var link = await userIdentityLinkQueryService.GetByProviderAndExternalIdAsync(
            "telegram", request.TelegramId.ToString(), cancellationToken);

        User user;

        if (link == null)
        {
            // Create dashboard User
            var displayName = !string.IsNullOrWhiteSpace(request.Username)
                ? request.Username!
                : $"{request.FirstName} {request.LastName}".Trim();

            user = new User
            {
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"tg_{request.TelegramId}" : displayName,
                Email = null,
                IsAdmin = false,
                IsBlocked = false,
                HasDashboardAccess = false,
                CreateDate = now,
                LastUpdate = now
            };

            user = await userCommandService.AddAsync(user, saveChanges: true, cancellationToken);
            logger.LogInformation("Dashboard user created for Telegram {TelegramId}", request.TelegramId);

            // Create identity link (mutate via command)
            var identityLink = new UserIdentityLink
            {
                UserId = user.Id,
                Provider = "telegram",
                ExternalId = request.TelegramId.ToString(),
                ProviderRowId = telegramBotUser.Id,
                CreateDate = now,
                LastUpdate = now
            };

            await userIdentityLinkCommandService.AddAsync(identityLink, saveChanges: true, cancellationToken);
            logger.LogInformation("UserIdentityLink created for user {UserId}", user.Id);

            // Assign default quota (read default plan via query, mutate via command)
            var defaultPlan = await quotaPlanQueryService.GetDefaultAsync(cancellationToken);
            if (defaultPlan != null)
            {
                var quotaPlan = new UserQuotaPlan
                {
                    UserId = user.Id,
                    QuotaPlanId = defaultPlan.Id,
                    EffectiveFrom = now,
                    EffectiveTo = null,
                    Note = "Auto-assigned default plan on Telegram registration",
                    CreateDate = now,
                    LastUpdate = now
                };

                await userQuotaPlanCommandService.AddAsync(quotaPlan, saveChanges: true, cancellationToken);
                logger.LogInformation("Default quota plan {PlanId} assigned to user {UserId}", defaultPlan.Id, user.Id);
            }
        }
        else
        {
            // Load user via query by link.UserId
            user = await userQueryService.GetByIdAsync(link.UserId, cancellationToken)
                   ?? throw new InvalidOperationException($"Linked user not found: {link.UserId}");
        }

        // 3) Build response
        var dto = user.Adapt<UserDto>();
        dto.Provider = "telegram";
        dto.ExternalId = request.TelegramId.ToString();
        dto.ProviderRowId = telegramBotUser.Id;

        return new UsersResponse { User = dto };
    }
    
    public Task<GetAllUsersResponse> GetAllUsers(CancellationToken cancellationToken)
    {
        userQueryService.GetAllAsync(cancellationToken);

        throw new NotImplementedException();
    }

    public Task<UsersResponse> GetUserById(GetUserByIdRequest request, CancellationToken cancellationToken)
    {
        userQueryService.GetByIdAsync(request.Id, cancellationToken);
        
        throw new NotImplementedException();
    }

    public Task<UsersResponse> GetUserByExternalId(GetUserByExternalIdRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}