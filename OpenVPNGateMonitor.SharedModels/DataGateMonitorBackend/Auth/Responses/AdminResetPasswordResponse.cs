namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

public sealed class AdminResetPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}
