namespace OpenVPNGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;

/// <summary>Notifications for errors occurring inside OpenVpnMicroserviceClient (SignalR, commands, reconnect).</summary>
public interface IOpenVpnMicroserviceNotificationService
{
    /// <summary>Failed to send command to microservice.</summary>
    Task NotifySendCommandFailed(int serverId, string? serverName, string? errorMessage, CancellationToken ct);

    /// <summary>Failed to reconnect SignalR to microservice.</summary>
    Task NotifyReconnectFailed(int serverId, string? serverName, string? errorMessage, CancellationToken ct);

    /// <summary>Failed to connect to microservice event hub (OpenVpnEventClient).</summary>
    Task NotifyEventHubConnectionFailed(int serverId, string? serverName, string? errorMessage, CancellationToken ct);
}
