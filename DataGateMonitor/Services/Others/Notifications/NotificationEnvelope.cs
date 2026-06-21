using DataGateMonitor.SharedModels.Notifications.Requests;

namespace DataGateMonitor.Services.Others.Notifications;

// Combines request + default channels policy for this notification
public sealed record NotificationEnvelope(
    NotifyAdminsRequest Request,
    IReadOnlyCollection<string> Channels // e.g. ["web"], ["web","telegram"]
);