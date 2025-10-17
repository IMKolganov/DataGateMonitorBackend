namespace OpenVPNGateMonitor.Services.Others.Notifications;

public class AppNotificationFacade(
    INotificationService notificationService,
    INotificationCatalog catalog) : IAppNotificationFacade
{
    public Task SystemExceptionAsync(Exception ex, CancellationToken ct)
    {
        var env = catalog.SystemException(ex);
        return notificationService.NotifyAdminsAsync(env.Request, env.Channels, ct);
    }

    public Task FileCreatedAsync(int actorUserId, string fileName, CancellationToken ct)
    {
        var env = catalog.FileCreated(actorUserId, fileName);
        return notificationService.NotifyAdminsAsync(env.Request, env.Channels, ct);
    }

    public Task CertIssuedAsync(int actorUserId, string commonName, int? serverId, CancellationToken ct)
    {
        var env = catalog.CertIssued(actorUserId, commonName, serverId);
        return notificationService.NotifyAdminsAsync(env.Request, env.Channels, ct);
    }

    public Task ServerDownAsync(int serverId, string serverName, CancellationToken ct)
    {
        var env = catalog.ServerDown(serverId, serverName);
        return notificationService.NotifyAdminsAsync(env.Request, env.Channels, ct);
    }

    public Task ServerUpAsync(int serverId, string serverName, CancellationToken ct)
    {
        var env = catalog.ServerUp(serverId, serverName);
        return notificationService.NotifyAdminsAsync(env.Request, env.Channels, ct);
    }
}