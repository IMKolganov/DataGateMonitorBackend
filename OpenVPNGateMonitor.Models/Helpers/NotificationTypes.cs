namespace OpenVPNGateMonitor.Models.Helpers;

public static class NotificationTypes
{
    public const string SystemException = "system.exception";
    public const string FileCreated     = "file.created";
    public const string CertIssued      = "cert.issued";
    public const string ServerDown      = "server.down";
    public const string ServerUp        = "server.up";
}