using OpenVPNGateMonitor.Models.Enums;

namespace OpenVPNGateMonitor.Models;

public class NotificationRecipient : BaseEntity<int>
{
    public int NotificationId { get; set; }
    public Notification Notification { get; set; } = default!;

    public int AdminUserId { get; set; }

    // e.g. "web", "telegram", "email"
    public string DeliveryChannel { get; set; } = "web";

    public DateTimeOffset? DeliveredAt { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;
    public DateTimeOffset? ReadAt { get; set; }
}