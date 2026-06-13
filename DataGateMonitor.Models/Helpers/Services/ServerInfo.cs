using DataGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;

namespace DataGateMonitor.Models.Helpers.Services;

public class ServerInfo//todo: move to nuget
{
    public string Status { get; set; } = string.Empty;
    public OpenVpnState? OpenVpnState { get; set; }
    public OpenVpnSummaryStats? OpenVpnSummaryStats { get; set; }
    public string Version { get; set; } = string.Empty;
}