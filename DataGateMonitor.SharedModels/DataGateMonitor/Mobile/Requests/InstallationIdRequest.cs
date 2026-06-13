using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Requests;

public class InstallationIdRequest
{
    [Required]
    public string InstallationId { get; set; } = string.Empty;
}