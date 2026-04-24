using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Requests;

public class GetGeoInfoRequest
{
    [Required(ErrorMessage = "ipAddress is required.")]
    [FromRoute(Name = "ipAddress")]
    public string IpAddress { get; set; } = string.Empty;
}