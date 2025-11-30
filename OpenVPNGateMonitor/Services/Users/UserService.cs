using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
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

public class UserService(
    IUserQueryService userQueryService,
    ITelegramBotUserQueryService telegramBotUserQueryService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IQuotaPlanQueryService quotaPlanQueryService,
    ICommandService<User, int> userCommandService,
    ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
    ICommandService<UserQuotaPlan, int> userQuotaPlanCommandService,
    ICommandService<TelegramBotUser, int> telegramBotUserCommandService,
    ILogger<UserService> logger
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
            var displayName =
                !string.IsNullOrWhiteSpace(request.Username)
                    ? request.Username!
                    : !string.IsNullOrWhiteSpace(request.FirstName)
                        ? $"{request.FirstName} {request.LastName}".Trim()
                        : $"tg_{request.TelegramId}";

            user = new User
            {
                DisplayName = displayName,
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
        var dto = await BuildUserDtoAsync(user, cancellationToken);
        // override with telegram context for this specific flow
        dto.Provider = "telegram";
        dto.ExternalId = request.TelegramId.ToString();
        dto.ProviderRowId = telegramBotUser.Id;

        return new UsersResponse { User = dto };
    }

    public async Task<GetAllUsersResponse> GetAllUsers(CancellationToken cancellationToken)
    {
        // Get all dashboard users
        var users = await userQueryService.GetAllAsync(cancellationToken);

        // NOTE: Simple and clear implementation.
        // If needed, this can be optimized later by adding a batch query for identity links.
        var dtos = new List<UserDto>(users.Count);
        foreach (var u in users)
        {
            var dto = await BuildUserDtoAsync(u, cancellationToken);
            dtos.Add(dto);
        }

        return new GetAllUsersResponse { Users = dtos };
    }

    public async Task<UsersResponse> GetUserById(GetUserByIdRequest request, CancellationToken cancellationToken)
    {
        // Load user by id or fail fast
        var user = await userQueryService.GetByIdAsync(request.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"User {request.Id} not found");

        var dto = await BuildUserDtoAsync(user, cancellationToken);
        return new UsersResponse { User = dto };
    }

    public async Task<UsersResponse> GetUserByExternalId(GetUserByExternalIdRequest request,
        CancellationToken cancellationToken)
    {
        // Resolve link first (provider + external id)
        var link = await userIdentityLinkQueryService
            .GetByExternalIdAsync(request.ExternalId, cancellationToken);

        if (link == null)
            throw new KeyNotFoundException(
                $"Identity link not found for externalId '{request.ExternalId}'");

        // Load user by link
        var user = await userQueryService.GetByIdAsync(link.UserId, cancellationToken)
                   ?? throw new InvalidOperationException($"Linked user not found: {link.UserId}");

        var dto = await BuildUserDtoAsync(user, cancellationToken);
        return new UsersResponse { User = dto };
    }

    // === Helpers ===

    private async Task<UserDto> BuildUserDtoAsync(User user, CancellationToken ct)
    {
        // Map base fields
        var dto = user.Adapt<UserDto>();

        // Try enrich with identity link (first link if multiple)
        // Requires IUserIdentityLinkQueryService.GetByUserIdAsync(int userId, CancellationToken ct)
        var link = await userIdentityLinkQueryService.GetByUserIdAsync(user.Id, ct);

        if (link != null)
        {
            dto.Provider = link.Provider;
            dto.ExternalId = link.ExternalId;
            dto.ProviderRowId = link.ProviderRowId;
        }
        else
        {
            // No link found — keep provider-related fields empty/neutral
            dto.Provider = string.Empty;
            dto.ExternalId = string.Empty;
            dto.ProviderRowId = null;
        }

        return dto;
    }
}
