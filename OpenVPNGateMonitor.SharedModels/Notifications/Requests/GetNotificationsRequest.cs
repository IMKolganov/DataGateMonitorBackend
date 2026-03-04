using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.Notifications.Requests;

public class GetNotificationsRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "pageSize must be greater than 0.")]
    public int PageSize { get; set; } = 10;
}
