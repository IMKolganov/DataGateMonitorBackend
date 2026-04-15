using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;

public class ByVpnServerIdRequest
{
    [Range(1, int.MaxValue)]
    [FromRoute(Name = "vpnServerId")]
    public int VpnServerId { get; set; }
}