using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;

public class ByTokenRequest
{
    [Required(ErrorMessage = "token is required.")]
    [FromRoute(Name = "token")]
    public string Token { get; set; } = string.Empty;
}