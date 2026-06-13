using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateVPNBot.OvpnFile.Requests;

public class ByTokenRequest
{
    [Required(ErrorMessage = "token is required.")]
    [FromQuery(Name = "token")]
    public string Token { get; set; } = string.Empty;
}