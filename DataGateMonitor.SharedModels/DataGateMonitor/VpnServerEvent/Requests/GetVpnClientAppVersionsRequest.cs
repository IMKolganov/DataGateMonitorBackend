using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Requests;

public class GetVpnClientAppVersionsRequest
{
    [Required(ErrorMessage = "vpnServerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "vpnServerId must be greater than 0.")]
    public int VpnServerId { get; set; }

    /// <summary>Optional filter: single OpenVPN common name (CN).</summary>
    public string? CommonName { get; set; }

    /// <summary>Optional filter: resolve all issued profile CNs for this external id on the server.</summary>
    public string? ExternalId { get; set; }
}
