namespace OpenVPNGateMonitor.SharedModels.Notifications.Responses;

public class GetAllNotificationsResponse
{
    public List<NotificationItemDto> Notifications { get; set; } = new();
}
