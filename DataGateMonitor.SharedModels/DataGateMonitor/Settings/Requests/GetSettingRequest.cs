using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Settings.Requests;

public class GetSettingRequest
{
    [Required(ErrorMessage = "Key is required.")]
    public string Key { get; set; } = string.Empty;
}