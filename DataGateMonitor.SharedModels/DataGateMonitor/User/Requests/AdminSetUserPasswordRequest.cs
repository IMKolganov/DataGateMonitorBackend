using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

public sealed class AdminSetUserPasswordRequest
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = default!;

    [MaxLength(256)]
    public string? Reason { get; set; }
}
