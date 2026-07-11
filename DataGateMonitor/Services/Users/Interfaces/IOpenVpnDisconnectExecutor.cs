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
}
