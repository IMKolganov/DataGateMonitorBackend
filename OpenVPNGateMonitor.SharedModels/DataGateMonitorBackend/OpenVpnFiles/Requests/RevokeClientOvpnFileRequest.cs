using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class RevokeClientOvpnFileRequest
{
    [Required(ErrorMessage = "serverId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "serverId must be greater than 0.")]
    public int VpnServerId { get; set; }
    [Required(ErrorMessage = "ovpnFileId is required.")]
    public int OvpnFileId { get; set; }
    [Required(ErrorMessage = "commonName is required.")]
    public string CommonName { get; set; } = string.Empty;
    public bool IsRevoked { get; set; } = false;
}