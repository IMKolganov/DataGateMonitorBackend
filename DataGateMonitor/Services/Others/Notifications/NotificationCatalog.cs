using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications;

public class NotificationCatalog : INotificationCatalog
{
    private static readonly string[] WebOnly        = ["web"];
    private static readonly string[] WebAndTelegram = ["web", "telegram"];

    public NotificationEnvelope SystemException(Exception ex)
    {
        var req = new NotificationRequest
        {
            Type = NotificationTypes.SystemException,
            Title = "Unhandled exception occurred",
            Message = $"{ex.GetType().Name}: {ex.Message}",
            Severity = NotificationSeverity.Error,
            Source = "middleware",
            PreferenceKind = ApplicationNotificationKind.AppUnhandledException
        };
        return new NotificationEnvelope(req, WebAndTelegram);
    }

    public NotificationEnvelope FileCreated(int actorUserId, string fileName)
    {
        var req = new NotificationRequest
        {
            Type = NotificationTypes.FileCreated,
            Title = "New file created",
            Message = $"User #{actorUserId} created file \"{fileName}\".",
            Severity = NotificationSeverity.Info,
            Source = "backend",
            ActorUserId = actorUserId,
            PreferenceKind = ApplicationNotificationKind.AppFileCreated
        };
        return new NotificationEnvelope(req, WebOnly);
    }

    public NotificationEnvelope CertIssued(int actorUserId, string commonName, int? serverId = null)
    {
        var req = new NotificationRequest
        {
            Type = NotificationTypes.CertIssued,
            Title = "Certificate issued",
            Message = $"Certificate for \"{commonName}\" has been issued.",
            Severity = NotificationSeverity.Info,
            Source = "backend",
            ActorUserId = actorUserId,
            ServerId = serverId,
            PreferenceKind = ApplicationNotificationKind.AppCertificateIssued
        };
        return new NotificationEnvelope(req, WebAndTelegram);
    }

    public NotificationEnvelope ServerDown(int serverId, string serverName)
    {
        var req = new NotificationRequest
        {
            Type = NotificationTypes.ServerDown,
            Title = "Server is DOWN",
            Message = $"VPN server \"{serverName}\" (id={serverId}) is unreachable.",
            Severity = NotificationSeverity.Critical,
            Source = "monitor",
            ServerId = serverId,
            PreferenceKind = ApplicationNotificationKind.AppServerMonitorDown
        };
        return new NotificationEnvelope(req, WebAndTelegram);
    }

    public NotificationEnvelope ServerUp(int serverId, string serverName)
    {
        var req = new NotificationRequest
        {
            Type = NotificationTypes.ServerUp,
            Title = "Server is UP",
            Message = $"VPN server \"{serverName}\" (id={serverId}) is reachable again.",
            Severity = NotificationSeverity.Info,
            Source = "monitor",
            ServerId = serverId,
            PreferenceKind = ApplicationNotificationKind.AppServerMonitorUp
        };
        return new NotificationEnvelope(req, WebAndTelegram);
    }

    public NotificationEnvelope UserRegistered(int userId, string displayName, string? login, string? email)
    {
        var loginPart = string.IsNullOrWhiteSpace(login) ? "" : $" Login: {login}.";
        var emailPart = string.IsNullOrWhiteSpace(email) ? "" : $" Email: {email}.";
        var req = new NotificationRequest
        {
            Type = NotificationTypes.UserRegistered,
            Title = "New user registered",
            Message = $"User #{userId} ({displayName}) registered.{loginPart}{emailPart}",
            Severity = NotificationSeverity.Info,
            Source = "auth",
            ActorUserId = userId,
            PreferenceKind = ApplicationNotificationKind.AppUserRegistered
        };
        return new NotificationEnvelope(req, WebAndTelegram);
    }

    public NotificationEnvelope UserPasswordChanged(int userId, string displayName, string login, string reason)
    {
        var req = new NotificationRequest
        {
            Type = NotificationTypes.UserPasswordChanged,
            Title = "User password changed",
            Message =
                $"User #{userId} ({displayName}), login \"{login}\", changed password. Source: {reason}.",
            Severity = NotificationSeverity.Warning,
            Source = "auth",
            ActorUserId = userId,
            PreferenceKind = ApplicationNotificationKind.AppUserPasswordChanged
        };
        return new NotificationEnvelope(req, WebAndTelegram);
    }
}
