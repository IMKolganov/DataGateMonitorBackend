using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;

public class GetUserByExternalIdRequest
{
    [Required(ErrorMessage = "ExternalId is required.")]
    public string ExternalId { get; set; } = string.Empty;
}