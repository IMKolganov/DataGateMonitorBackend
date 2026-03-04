using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Requests;

public class CreateOrUpdateTagRequest
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;
}
