using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class DownloadFileByCnRequest
{
    [Required(ErrorMessage = "issuedOvpnFileId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "issuedOvpnFileId must be greater than 0.")]
    public int IssuedOvpnFileId { get; set; }

    [Required(ErrorMessage = "commonName is required.")]
    public string CommonName { get; set; } = string.Empty;
}