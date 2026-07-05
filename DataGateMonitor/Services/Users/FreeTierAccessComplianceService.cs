using DataGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.Services.Users.Interfaces;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.Users;

public sealed class FreeTierAccessComplianceService(
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IQuotaPlanQueryService quotaPlanQueryService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    ITelegramChannelMembershipChecker telegramChannelMembershipChecker,
    IAppNotificationFacade appNotificationFacade,
    IOptions<TelegramChannelSettings> channelOptions,
    ILogger<FreeTierAccessComplianceService> logger) : IFreeTierAccessComplianceService
{
    private const string GoogleProvider = "google";
    private const string TelegramProvider = "telegram";

    public async Task<FreeTierAccessComplianceResult> AuditAndNotifyIfNeededByTelegramIdAsync(
        long telegramId,
        string context,
        bool? isChannelSubscribed = null,
        CancellationToken ct = default)
    {
        if (telegramId <= 0)
        {
            return new FreeTierAccessComplianceResult
            {
                IsApplicable = false,
                IsCompliant = true,
                TelegramId = telegramId,
            };
        }

        var link = await userIdentityLinkQueryService.GetByProviderAndExternalId(
            TelegramProvider,
            telegramId.ToString(),
            ct);

        if (link is not { UserId: > 0 })
        {
            return new FreeTierAccessComplianceResult
            {
                IsApplicable = false,
                IsCompliant = true,
                TelegramId = telegramId,
            };
        }

        var result = await AuditAndNotifyIfNeededAsync(link.UserId, context, isChannelSubscribed, ct);
        if (telegramId > 0)
            result.TelegramId = telegramId;
        return result;
    }

    public async Task<FreeTierAccessComplianceResult> AuditAndNotifyIfNeededAsync(
        int userId,
        string context,
        bool? isChannelSubscribed = null,
        CancellationToken ct = default)
    {
        var activeAssignment = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
        if (activeAssignment is null)
        {
            return new FreeTierAccessComplianceResult
            {
                IsApplicable = false,
                IsCompliant = true,
                UserId = userId,
            };
        }

        var plan = await quotaPlanQueryService.GetById(activeAssignment.QuotaPlanId, ct);
        if (plan is null || !QuotaPlanNames.IsFreeOrDefault(plan.Name))
        {
            return new FreeTierAccessComplianceResult
            {
                IsApplicable = false,
                IsCompliant = true,
                UserId = userId,
                ActivePlanName = plan?.Name,
            };
        }

        var links = await userIdentityLinkQueryService.GetListByUserId(userId, ct);
        var isMerged = IsMergedAccount(links);
        var telegramId = TryGetTelegramId(links);

        var channelSubscribed = isChannelSubscribed;
        if (channelSubscribed != true && telegramId is > 0)
            channelSubscribed = await telegramChannelMembershipChecker.IsSubscribedAsync(telegramId.Value, ct);

        var isCompliant = isMerged || channelSubscribed == true;
        if (isCompliant)
        {
            return new FreeTierAccessComplianceResult
            {
                IsApplicable = true,
                IsCompliant = true,
                IsMergedAccount = isMerged,
                IsChannelSubscribed = channelSubscribed == true,
                ActivePlanName = plan.Name,
                UserId = userId,
                TelegramId = telegramId,
            };
        }

        try
        {
            await appNotificationFacade.FreeTierAccessNonCompliant(
                userId,
                plan.Name,
                telegramId,
                isMerged,
                channelSubscribed == true,
                context,
                channelOptions.Value.RequiredChannelChatId,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify admins about Free/Default access violation for user {UserId}", userId);
        }

        logger.LogWarning(
            "User {UserId} on plan {PlanName} is not merged and not subscribed to {Channel}. Context={Context}",
            userId,
            plan.Name,
            channelOptions.Value.RequiredChannelChatId,
            context);

        return new FreeTierAccessComplianceResult
        {
            IsApplicable = true,
            IsCompliant = false,
            IsMergedAccount = isMerged,
            IsChannelSubscribed = channelSubscribed == true,
            ActivePlanName = plan.Name,
            UserId = userId,
            TelegramId = telegramId,
            AdminsNotified = true,
        };
    }

    internal static bool IsMergedAccount(IReadOnlyCollection<UserIdentityLink> links)
    {
        var hasTelegram = links.Any(l =>
            string.Equals(l.Provider, TelegramProvider, StringComparison.OrdinalIgnoreCase));
        var hasGoogle = links.Any(l =>
            string.Equals(l.Provider, GoogleProvider, StringComparison.OrdinalIgnoreCase));
        return hasTelegram && hasGoogle;
    }

    internal static long? TryGetTelegramId(IReadOnlyCollection<UserIdentityLink> links)
    {
        var externalId = links
            .FirstOrDefault(l => string.Equals(l.Provider, TelegramProvider, StringComparison.OrdinalIgnoreCase))
            ?.ExternalId;

        return long.TryParse(externalId, out var telegramId) && telegramId > 0 ? telegramId : null;
    }
}
