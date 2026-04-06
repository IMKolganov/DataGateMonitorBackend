using System.ComponentModel.DataAnnotations;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.Notifications.Requests;

public class GetNotificationsRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "pageSize must be greater than 0.")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Unread only (<c>false</c>), read only (<c>true</c>), or all (<c>null</c>).
    /// </summary>
    public bool? IsRead { get; set; }

    /// <summary>
    /// When set and non-empty, only these <see cref="NotificationSeverity"/> values are returned.
    /// Query: repeat the parameter, e.g. <c>?severities=Warning&amp;severities=Error</c>.
    /// </summary>
    public NotificationSeverity[]? Severities { get; set; }

    /// <summary>
    /// Optional exact match on notification type (e.g. <c>server.down</c>, <c>cert.issued</c>).
    /// </summary>
    [MaxLength(256)]
    public string? Type { get; set; }
}
