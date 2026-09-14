using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;

/// <summary>v2 paged certificates for a VPN server.</summary>
public class GetCertificatesV2Request
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "vpnServerId must be greater than 0.")]
    public int VpnServerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "pageSize must be between 1 and 500.")]
    public int PageSize { get; set; } = 20;
}
