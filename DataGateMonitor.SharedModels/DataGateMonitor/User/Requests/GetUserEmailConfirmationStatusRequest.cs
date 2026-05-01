using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

public class GetUserEmailConfirmationStatusRequest
{
    [Required(ErrorMessage = "id is required.")]
    [FromRoute(Name = "id")]
    public int Id { get; set; }
}
