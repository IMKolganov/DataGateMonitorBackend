namespace DataGateMonitor.Services.Others.Notifications;

public interface INotificationCatalog
{
    NotificationEnvelope SystemException(Exception ex);
    NotificationEnvelope FileCreated(int actorUserId, string fileName);
    NotificationEnvelope CertIssued(int actorUserId, string commonName, int? serverId = null);
    NotificationEnvelope ServerDown(int serverId, string serverName);
    NotificationEnvelope ServerUp(int serverId, string serverName);

    NotificationEnvelope UserRegistered(int userId, string displayName, string? login, string? email, string? registrationSource = null);

    NotificationEnvelope UserPasswordChanged(int userId, string displayName, string login, string reason);
}