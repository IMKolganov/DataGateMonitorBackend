using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public abstract class BaseEntity<TKey> : IBaseEntity
{
    [Key]
    public TKey Id { get; set; } = default!;

    public DateTimeOffset CreateDate { get; set; }
    
    public DateTimeOffset LastUpdate { get; set; }
}