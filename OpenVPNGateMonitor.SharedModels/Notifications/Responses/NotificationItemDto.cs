using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.Notifications.Responses;

public class NotificationItemDto
{
    public int Id { get; set; }
    public string Type { get; set; } = null!;
    public NotificationSeverity Severity { get; set; }
    public string Title { get; set; } = null!;
    public string? Message { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
