using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Mobile.Requests;

public class InstallationIdRequest
{
    [Required]
    public string InstallationId { get; set; } = string.Empty;
}