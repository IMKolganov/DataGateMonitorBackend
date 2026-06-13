using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

public class Tag : BaseEntity<int>
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;
}
