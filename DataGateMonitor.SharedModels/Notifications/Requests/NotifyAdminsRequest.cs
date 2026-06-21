using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.Notifications.Requests;

/// <summary>
/// Body for <c>POST api/notifications/notify-admins</c>.
/// </summary>
public class NotifyAdminsRequest
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    public string Source { get; set; } = "backend";

    public int? ServerId { get; set; }

    public int? ActorUserId { get; set; }

    public int? RelatedClientId { get; set; }

    public string? CorrelationId { get; set; }

    public string? DedupKey { get; set; }

    /// <summary>When set, delivery is skipped if this kind is disabled in admin notification preferences.</summary>
    public ApplicationNotificationKind? PreferenceKind { get; set; }
}
