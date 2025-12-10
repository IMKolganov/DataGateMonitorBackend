namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;

public sealed class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}