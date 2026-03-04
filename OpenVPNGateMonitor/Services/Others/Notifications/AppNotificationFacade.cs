namespace OpenVPNGateMonitor.Services.Others.Notifications;

public class AppNotificationFacade(
    INotificationService notificationService,
    INotificationCatalog catalog) : IAppNotificationFacade
{
    public Task SystemException(Exception ex, CancellationToken ct)
    {
        var env = catalog.SystemException(ex);
        return notificationService.NotifyAdmins(env.Request, env.Channels, ct);
    }

    public Task FileCreated(int actorUserId, string fileName, CancellationToken ct)
    {
        var env = catalog.FileCreated(actorUserId, fileName);
        return notificationService.NotifyAdmins(env.Request, env.Channels, ct);
    }

    public Task CertIssued(int actorUserId, string commonName, int? serverId, CancellationToken ct)
    {
        var env = catalog.CertIssued(actorUserId, commonName, serverId);
        return notificationService.NotifyAdmins(env.Request, env.Channels, ct);
    }

    public Task ServerDown(int serverId, string serverName, CancellationToken ct)
    {
        var env = catalog.ServerDown(serverId, serverName);
        return notificationService.NotifyAdmins(env.Request, env.Channels, ct);
    }

    public Task ServerUp(int serverId, string serverName, CancellationToken ct)
    {
        var env = catalog.ServerUp(serverId, serverName);
        return notificationService.NotifyAdmins(env.Request, env.Channels, ct);
    }
}