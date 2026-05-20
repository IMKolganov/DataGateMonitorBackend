namespace DataGateMonitor.Services.Others.Notifications;

public interface IAppNotificationFacade
{
    Task SystemException(Exception ex, CancellationToken ct);
    Task FileCreated(int actorUserId, string fileName, CancellationToken ct);
    Task CertIssued(int actorUserId, string commonName, int? serverId, CancellationToken ct);
    Task ServerDown(int serverId, string serverName, CancellationToken ct);
    Task ServerUp(int serverId, string serverName, CancellationToken ct);

    Task UserRegistered(int userId, string displayName, string? login, string? email, string? registrationSource, CancellationToken ct);

    Task UserPasswordChanged(int userId, string displayName, string login, string reason, CancellationToken ct);
}