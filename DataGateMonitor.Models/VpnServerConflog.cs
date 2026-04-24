using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>
/// Log of microservice INFO responses (api/info). New row only when payload changed.
/// </summary>
public class VpnServerConflog : BaseEntity<int>
{
    /// <summary>VPN server id when request was made by server id; null when requested by URL only.</summary>
    public int? VpnServerId { get; set; }

    /// <summary>Base URL that was requested (e.g. https://host:port/).</summary>
    [Required, MaxLength(512)]
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>Full INFO response as JSON.</summary>
    [Required]
    public string PayloadJson { get; set; } = string.Empty;
}
