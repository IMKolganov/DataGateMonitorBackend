using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Services.Others.Models;

public class NotificationRequest
{
    public string Type {get; set;} = string.Empty;
    public string Title {get; set;} = string.Empty;
    public string Message {get; set;} = string.Empty;
    public NotificationSeverity Severity = NotificationSeverity.Info;
    public string Source { get; set; } = "backend";
    public int? ServerId = null;
    public int? ActorUserId = null;
    public int? RelatedClientId = null;
    public string? CorrelationId = null;
    public string? DedupKey = null;
}