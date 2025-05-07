namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class RevokeOvpnFileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}