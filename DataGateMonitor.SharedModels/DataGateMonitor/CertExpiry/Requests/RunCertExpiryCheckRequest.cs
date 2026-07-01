namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;

public class RunCertExpiryCheckRequest
{
    /// <summary>When set, only this OpenVPN server is checked. When null, all eligible servers are checked.</summary>
    public int? VpnServerId { get; set; }

    /// <summary>When true, admin notifications are sent for findings (scheduled runs always notify).</summary>
    public bool SendNotifications { get; set; }
}
