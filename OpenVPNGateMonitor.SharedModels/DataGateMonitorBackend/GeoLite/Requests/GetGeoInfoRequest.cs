using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Requests;

public class GetGeoInfoRequest
{
    [Required(ErrorMessage = "IpAddress is required.")]
    public int IpAddress { get; set; }
}