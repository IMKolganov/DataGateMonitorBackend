using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Request;

public class VpnServerStatisticRequest
{
    [Required(ErrorMessage = "VpnServerId is required.")]
    [FromRoute(Name = "vpnServerId")]
    public int VpnServerId { get; set; }
}