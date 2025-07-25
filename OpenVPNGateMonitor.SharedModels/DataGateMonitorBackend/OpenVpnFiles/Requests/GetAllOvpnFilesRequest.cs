using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class GetAllOvpnFilesRequest
{
    [Required]
    [FromRoute(Name = "vpnServerId")]
    public int VpnServerId { get; set; }
}