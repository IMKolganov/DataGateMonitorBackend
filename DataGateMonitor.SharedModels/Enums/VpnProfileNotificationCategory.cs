namespace DataGateMonitor.SharedModels.Enums;

/// <summary>Admin notification group for VPN profile APIs (OpenVPN files / Xray client links).</summary>
public enum VpnProfileNotificationCategory
{
    Read = 0,
    Mutate = 1,
    Download = 2
}
