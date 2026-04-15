using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.Notifications.Responses;

public class GetNotificationsResponse
{
    public PagedResponse<NotificationItemDto> Notifications { get; set; } = new();
}
