using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Requests;

public class GetClientStatusRequest
{
    [Required(ErrorMessage = "vpnServerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "vpnServerId must be greater than 0.")]
    [FromRoute(Name = "vpnServerId")]
    public int VpnServerId { get; set; }
}