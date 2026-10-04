using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.Notifications.Responses;

public class NotificationDeliveryDto
{
    public string Channel { get; set; } = null!;
    public DeliveryStatus Status { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
}
