using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Settings.Requests;

public class GetSettingRequest
{
    [Required(ErrorMessage = "Key is required.")]
    public string Key { get; set; } = string.Empty;
}