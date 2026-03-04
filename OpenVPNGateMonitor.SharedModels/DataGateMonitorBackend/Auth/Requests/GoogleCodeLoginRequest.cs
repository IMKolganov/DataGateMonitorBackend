namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;

public sealed class GoogleCodeLoginRequest
{
    public string Code { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}