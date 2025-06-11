using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;

public class OpenVpnServerEventLogDto
{
    public int Id { get; set; }
    [Required]
    public int VpnServerId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? RealAddress { get; set; }
    public string? VirtualAddress { get; set; }
    public DateTime? ConnectedSince { get; set; }
    public string? Message { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
}