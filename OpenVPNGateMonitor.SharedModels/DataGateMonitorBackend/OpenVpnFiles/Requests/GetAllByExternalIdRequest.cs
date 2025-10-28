using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class GetAllByExternalIdRequest
{
    [Required(ErrorMessage = "externalId is required.")]
    [FromRoute(Name = "externalId")]
    public string ExternalId { get; set; } = string.Empty;
}