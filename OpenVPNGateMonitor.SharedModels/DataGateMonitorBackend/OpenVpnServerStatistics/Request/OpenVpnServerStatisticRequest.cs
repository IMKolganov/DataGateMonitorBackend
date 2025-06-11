using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Request;

public class OpenVpnServerStatisticRequest
{
    [Required(ErrorMessage = "VpnServerId is required.")]
    public int VpnServerId { get; set; }
}