using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class ByVpnServerIdRequest
{
    [Range(1, int.MaxValue)]
    [FromRoute(Name = "vpnServerId")]
    public int VpnServerId { get; set; }
}