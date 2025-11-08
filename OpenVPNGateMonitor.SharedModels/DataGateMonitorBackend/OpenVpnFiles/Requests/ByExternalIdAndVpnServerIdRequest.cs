using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class ByExternalIdAndVpnServerIdRequest
{
    [Required(ErrorMessage = "vpnServerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "vpnServerId must be greater than 0.")]
    [FromRoute(Name = "vpnServerId")]
    public int VpnServerId { get; set; }

    [Required(ErrorMessage = "externalId is required.")]
    [FromRoute(Name = "externalId")]
    public string ExternalId { get; set; } = string.Empty;
}