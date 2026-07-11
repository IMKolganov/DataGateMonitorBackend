using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Dto;

/// <summary>Append-only audit entry for every OpenVPN kill (enforcement job or manual admin action).</summary>
public sealed class FreeTierDisconnectLogEntryDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserDisplayName { get; set; }

    public int VpnServerId { get; set; }
    public string? VpnServerName { get; set; }
    public string CommonName { get; set; } = string.Empty;

    public DisconnectReason Reason { get; set; }
    public int? InitiatedByUserId { get; set; }
    public string? InitiatedByDisplayName { get; set; }

    public bool RevokeRequested { get; set; }
    public bool? RevokeSucceeded { get; set; }
    public bool KillSucceeded { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
