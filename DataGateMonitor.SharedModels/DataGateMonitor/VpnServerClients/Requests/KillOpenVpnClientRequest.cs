using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;

/// <summary>Admin-triggered disconnect of a connected OpenVPN client from the clients table.</summary>
public class KillOpenVpnClientRequest
{
    [Required(ErrorMessage = "vpnServerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "vpnServerId must be greater than 0.")]
    public int VpnServerId { get; set; }

    [Required(ErrorMessage = "commonName is required.")]
    public string CommonName { get; set; } = string.Empty;

    /// <summary>OpenVPN management client id, when known; falls back to killing by CommonName.</summary>
    public long? ManagementClientId { get; set; }

    /// <summary>
    /// When true, also revokes the client's certificate/ovpn file so it cannot silently reconnect
    /// with the same profile. When false, only drops the current session.
    /// </summary>
    public bool RevokeCertificate { get; set; }
}
