using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Request;

public class OpenVpnServerStatisticRequest
{
    [Required(ErrorMessage = "VpnServerId is required.")]
    [FromRoute(Name = "vpnServerId")]
    public int VpnServerId { get; set; }
}