using OpenVPNGateMonitor.Services.Others.Models;

namespace OpenVPNGateMonitor.Services.Others.Notifications;

// Combines request + default channels policy for this notification
public sealed record NotificationEnvelope(
    NotificationRequest Request,
    IReadOnlyCollection<string> Channels // e.g. ["web"], ["web","telegram"]
);