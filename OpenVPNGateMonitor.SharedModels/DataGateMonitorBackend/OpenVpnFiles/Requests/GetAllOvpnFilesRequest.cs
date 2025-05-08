using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class GetAllOvpnFilesRequest
{
    [Required]
    public int VpnServerId { get; set; }
}