using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;

public class GetUserByIdRequest
{
    [Required(ErrorMessage = "Id is required.")]
    public int Id { get; set; }
}