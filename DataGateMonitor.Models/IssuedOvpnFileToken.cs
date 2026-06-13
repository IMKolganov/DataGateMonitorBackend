using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataGateMonitor.Models;

public class IssuedOvpnFileToken : BaseEntity<int>
{
    [Required]
    public int IssuedOvpnFileId { get; set; }

    [ForeignKey(nameof(IssuedOvpnFileId))]
    public IssuedOvpnFile IssuedOvpnFile { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    [Required]
    public bool IsUsed { get; set; } = false;

    public string? Purpose { get; set; }
}