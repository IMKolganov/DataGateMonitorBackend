namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>
/// Body for Development-only <c>POST /api/auth/dev/token</c>.
/// </summary>
public sealed class DevTokenRequest
{
    /// <summary>
    /// <c>App</c>, <c>Admin</c>, or <c>OpenVpn</c> (microservice JWT for DataGateOpenVpnManager).
    /// Default: <c>OpenVpn</c>.
    /// </summary>
    public string Role { get; set; } = "OpenVpn";
}
