using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class OpenVpnServerEventLog: BaseEntity<int>
{
    [Required]
    public int VpnServerId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? RealAddress { get; set; }
    public string? VirtualAddress { get; set; }
    public DateTime? ConnectedSince { get; set; }
    public string? Message { get; set; }
    public string RawJson { get; set; } = string.Empty;
}