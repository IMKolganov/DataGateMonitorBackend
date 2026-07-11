using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.EmailTemplates;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.Services.Users.Interfaces;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.Users;

public sealed class FreeTierGraceDisconnectNotifier(
    IUserQueryService userQueryService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IFreeTierAccessComplianceService freeTierAccessComplianceService,
    ITelegramDirectMessageSender telegramDirectMessageSender,
    IEmailSenderService emailSenderService,
    ISystemTransactionalEmailService systemTransactionalEmailService,
    IOptions<TelegramChannelSettings> channelOptions,
    ILogger<FreeTierGraceDisconnectNotifier> logger) : IFreeTierGraceDisconnectNotifier
{
    public async Task NotifyAsync(int userId, CancellationToken ct = default)
    {
        try
        {
            var user = await userQueryService.GetById(userId, ct);
            if (user is null)
                return;

            var links = await userIdentityLinkQueryService.GetListByUserId(userId, ct);
            var telegramId = FreeTierAccessComplianceService.TryGetTelegramId(links);
            var requiredChannel = channelOptions.Value.RequiredChannelChatId;

            if (telegramId is > 0)
            {
                var text = BuildTelegramMessage(requiredChannel);
                if (await telegramDirectMessageSender.TrySendMessageAsync(telegramId.Value, text, ct))
                    return;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                logger.LogInformation(
                    "No Telegram or email channel available to notify user {UserId} about a free-tier grace-period disconnect.",
                    userId);
                return;
            }

            var evaluation = await freeTierAccessComplianceService.EvaluateAccessForEnforcementAsync(userId, ct);
            var planName = evaluation.ActivePlanName ?? QuotaPlanNames.Free;

            var (subject, bodyHtml) = await systemTransactionalEmailService.GetFreeTierGraceDisconnectedAsync(
                planName, requiredChannel, ct);
            await emailSenderService.SendAsync(user.Email, subject, bodyHtml, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify user {UserId} about a free-tier grace-period disconnect.", userId);
        }
    }

    private static string BuildTelegramMessage(string requiredChannel)
        => "⚠️ You were disconnected from the VPN. Your grace period ended and your account still isn't " +
           $"compliant — subscribe to {requiredChannel} or link your account in the app, then reconnect.";
}
