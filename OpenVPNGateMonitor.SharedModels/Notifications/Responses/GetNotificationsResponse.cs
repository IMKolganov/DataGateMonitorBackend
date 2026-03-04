using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.SharedModels.Notifications.Responses;

public class GetNotificationsResponse
{
    public PagedResponse<NotificationItemDto> Notifications { get; set; } = new();
}
