using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerOvpnFileConfig.Requests;

public class AddOrUpdateOvpnFileConfigRequest
{
    [Required(ErrorMessage = "serverId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "serverId must be greater than 0.")]
    public int VpnServerId { get; set; }

    [Required(ErrorMessage = "vpnServerIp is required.")]
    public string VpnServerIp { get; set; } = string.Empty;

    public int VpnServerPort { get; set; }
    public string ConfigTemplate { get; set; } = string.Empty;

    public bool AutoDetectServerSettings { get; set; } = true;
}