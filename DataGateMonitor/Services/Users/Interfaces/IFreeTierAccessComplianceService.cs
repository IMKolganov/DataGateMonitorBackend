namespace DataGateMonitor.Services.Users.Interfaces;

using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public interface IFreeTierAccessComplianceService
{
    /// <summary>
    /// Read-only status for client apps. Does not notify admins or start a grace period.
    /// </summary>
    Task<FreeTierAccessStatusResponse> GetStatusAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Client apps call this right after establishing a VPN connection. Starts/refreshes the grace
    /// window for a non-compliant user (same effect as <see cref="AuditAndNotifyIfNeededAsync"/>) and
    /// returns the resulting status, including how long the grace window has left.
    /// </summary>
    Task<FreeTierAccessStatusResponse> RegisterConnectionAsync(int userId, string context, CancellationToken ct = default);

    /// <summary>
    /// Raw compliance evaluation with no grace period applied — the exact rule the OpenVPN session
    /// enforcement job uses to decide who to kill (<see cref="ShouldEnforceOpenVpnDisconnectAsync"/>).
    /// Used to build the "who currently qualifies for enforcement" admin view so it matches reality.
    /// </summary>
    Task<FreeTierAccessComplianceResult> EvaluateAccessForEnforcementAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// When the user has an active Free/Default plan, they must be a merged Telegram+Google account
    /// or subscribed to the required Telegram channel. Otherwise admins are notified.
    /// </summary>
    /// <param name="isChannelSubscribed">When set by the bot after a live getChatMember check.</param>
    Task<FreeTierAccessComplianceResult> AuditAndNotifyIfNeededAsync(
        int userId,
        string context,
        bool? isChannelSubscribed = null,
        CancellationToken ct = default);

    Task<FreeTierAccessComplianceResult> AuditAndNotifyIfNeededByTelegramIdAsync(
        long telegramId,
        string context,
        bool? isChannelSubscribed = null,
        CancellationToken ct = default);

    /// <summary>
    /// Strict enforcement check for OpenVPN: Free/Default users who are neither merged nor channel-subscribed.
    /// Ignores grace period.
    /// </summary>
    Task<bool> ShouldEnforceOpenVpnDisconnectAsync(int userId, CancellationToken ct = default);
}
