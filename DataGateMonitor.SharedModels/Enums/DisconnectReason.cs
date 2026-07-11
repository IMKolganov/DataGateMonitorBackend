namespace DataGateMonitor.SharedModels.Enums;

/// <summary>Why an OpenVPN client session was disconnected (kill sent to the management interface).</summary>
public enum DisconnectReason
{
    /// <summary>Free-tier compliance background job.</summary>
    Enforcement,

    /// <summary>Admin-triggered disconnect from the connected clients table.</summary>
    Manual
}
