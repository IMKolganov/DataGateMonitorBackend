namespace DataGateMonitor.SharedModels.Enums;

/// <summary>Which VPN profile API produced the event (OpenVPN .ovpn vs Xray client links).</summary>
public enum VpnProfileNotificationStack
{
    OpenVpn = 0,
    Xray = 1
}
