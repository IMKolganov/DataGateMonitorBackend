using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Users.Interfaces;

public sealed class OpenVpnDisconnectRequest
{
    public required VpnServer Server { get; init; }
    public required VpnServerClient Client { get; init; }
    public int? UserId { get; init; }
    public string? UserDisplayNameSnapshot { get; init; }
    public required DisconnectReason Reason { get; init; }
    public int? InitiatedByUserId { get; init; }
    public bool RevokeCertificate { get; init; }
}

/// <summary>
/// Shared kill (+ optional certificate revoke) + audit-log path used by both the automated free-tier
/// enforcement job and the admin manual "Kill" / "Kill + Revoke" action on the connected clients table.
/// </summary>
public interface IOpenVpnDisconnectExecutor
{
    Task<KillOpenVpnClientResponse> ExecuteAsync(OpenVpnDisconnectRequest request, CancellationToken ct = default);

    /// <summary>
    /// Same as <see cref="ExecuteAsync"/>, but also returns the id of the <c>FreeTierDisconnectLog</c>
    /// row that was written (null if the write failed), so a caller can precisely correlate a later
    /// <see cref="UpdateNotificationOutcomeAsync"/> call to this exact disconnect event.
    /// </summary>
    Task<(KillOpenVpnClientResponse Response, int? DisconnectLogId)> ExecuteWithLogIdAsync(
        OpenVpnDisconnectRequest request, CancellationToken ct = default);

    /// <summary>
    /// Records whether the user was told about a disconnect on the <c>FreeTierDisconnectLog</c> row
    /// identified by <paramref name="disconnectLogId"/> (from <see cref="ExecuteWithLogIdAsync"/>).
    /// Best-effort: silently no-ops if the row no longer exists.
    /// </summary>
    Task UpdateNotificationOutcomeAsync(
        int disconnectLogId,
        string? notificationChannel,
        bool notificationSent,
        CancellationToken ct = default);
}
