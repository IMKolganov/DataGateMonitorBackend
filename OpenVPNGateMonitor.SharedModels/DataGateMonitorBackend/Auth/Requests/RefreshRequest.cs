namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;

    public string? DeviceId { get; set; }
    public string? UserAgent { get; set; }
}