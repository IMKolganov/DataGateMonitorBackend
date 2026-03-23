using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;

public class OpenVpnMicroserviceNotificationService(INotificationService notifications)
    : IOpenVpnMicroserviceNotificationService
{
    private static readonly string[] ErrorChannels = ["web", "telegram"];
    private const string Source = "openvpn-microservice-client";

    public Task NotifySendCommandFailed(int serverId, string? serverName, string? errorMessage, CancellationToken ct)
        => Notify("microservice.send-command-failed", "Failed to send command to OpenVPN microservice",
            Detail(serverId, serverName) + (string.IsNullOrEmpty(errorMessage) ? "" : $"; Error={errorMessage}"),
            serverId, NotificationSeverity.Error, ErrorChannels, ct);

    public Task NotifyReconnectFailed(int serverId, string? serverName, string? errorMessage, CancellationToken ct)
        => Notify("microservice.reconnect-failed", "Failed to reconnect to OpenVPN microservice",
            Detail(serverId, serverName) + (string.IsNullOrEmpty(errorMessage) ? "" : $"; Error={errorMessage}"),
            serverId, NotificationSeverity.Error, ErrorChannels, ct);

    public Task NotifyEventHubConnectionFailed(int serverId, string? serverName, string? errorMessage, CancellationToken ct)
        => Notify("microservice.event-hub-connection-failed", "Failed to connect to OpenVPN microservice event hub",
            Detail(serverId, serverName) + (string.IsNullOrEmpty(errorMessage) ? "" : $"; Error={errorMessage}"),
            serverId, NotificationSeverity.Error, ErrorChannels, ct);

    public Task NotifyProxyClientLookupFailed(int serverId, string? serverName, string detail, NotificationSeverity severity, CancellationToken ct)
        => Notify("microservice.proxy-client-lookup-failed", "Proxy client lookup failed",
            Detail(serverId, serverName) + (string.IsNullOrEmpty(detail) ? "" : $"; {detail}"),
            serverId, severity, ErrorChannels, ct);

    private static string Detail(int serverId, string? serverName)
        => $"ServerId={serverId}" + (string.IsNullOrEmpty(serverName) ? "" : $"; Name={serverName}");

    private Task Notify(string type, string title, string message, int serverId, NotificationSeverity severity,
        string[] channels, CancellationToken ct)
        => notifications.NotifyAdmins(new NotificationRequest
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Source = Source,
            ServerId = serverId
        }, channels, ct);
}
