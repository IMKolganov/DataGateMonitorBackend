using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;

public class ByTokenRequest
{
    [Required(ErrorMessage = "token is required.")]
    [FromRoute(Name = "token")]
    public string Token { get; set; } = string.Empty;
}