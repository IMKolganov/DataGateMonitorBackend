using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class Tag : BaseEntity<int>
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;
}
