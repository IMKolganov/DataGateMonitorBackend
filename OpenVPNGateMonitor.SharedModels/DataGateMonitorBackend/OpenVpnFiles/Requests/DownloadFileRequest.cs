using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class DownloadFileRequest
{
    [Required(ErrorMessage = "issuedOvpnFileId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "issuedOvpnFileId must be greater than 0.")]
    public int IssuedOvpnFileId { get; set; }

    [Required(ErrorMessage = "vpnServerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "vpnServerId must be greater than 0.")]
    public int VpnServerId { get; set; }
}