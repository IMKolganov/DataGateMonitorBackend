namespace OpenVPNGateMonitor.Services.Others.Notifications;

public interface IAppNotificationFacade
{
    Task SystemExceptionAsync(Exception ex, CancellationToken ct);
    Task FileCreatedAsync(int actorUserId, string fileName, CancellationToken ct);
    Task CertIssuedAsync(int actorUserId, string commonName, int? serverId, CancellationToken ct);
    Task ServerDownAsync(int serverId, string serverName, CancellationToken ct);
    Task ServerUpAsync(int serverId, string serverName, CancellationToken ct);
}