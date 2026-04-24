using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnWebSocket.Requests;

public class GetWebSocketByServerIdRequest
{
    [Required(ErrorMessage = "openVpnServerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "openVpnServerId must be greater than 0.")]
    public int VpnServerId { get; set; }
}