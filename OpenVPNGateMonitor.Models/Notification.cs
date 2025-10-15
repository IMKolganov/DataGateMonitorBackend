using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Models;

public class Notification : BaseEntity<int>
{
    // e.g. "server.down", "server.up", "cert.issued", "user.created"
    public string Type { get; set; } = default!;

    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Source { get; set; } = "backend";

    public string? CorrelationId { get; set; }
    public string? DedupKey { get; set; }

    public int? ServerId { get; set; }           // link to OpenVPN server
    public int? ActorUserId { get; set; }        // who triggered the action (if any)
    public int? RelatedClientId { get; set; }    // e.g., OpenVPN client/user id

    public bool IsArchived { get; set; }
}