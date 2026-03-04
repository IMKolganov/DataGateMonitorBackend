namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;

public class LoginRequest
{
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
}
