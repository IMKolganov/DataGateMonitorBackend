using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

public class GetUserByExternalIdRequest
{
    [Required(ErrorMessage = "externalId is required.")]
    [FromRoute(Name = "externalId")]
    public string ExternalId { get; set; } = string.Empty;
}