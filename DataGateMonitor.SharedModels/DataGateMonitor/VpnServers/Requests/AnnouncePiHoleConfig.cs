using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;

/// <summary>Node-provided Pi-hole settings. Credentials are never included in discovery responses.</summary>
public sealed class AnnouncePiHoleConfig
{
    [Required, MaxLength(512)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MaxLength(1024)]
    public string AppPassword { get; set; } = string.Empty;

    [Range(10, 86400)]
    public int PollIntervalSeconds { get; set; } = 60;

    [Range(1, 10000)]
    public int BatchSize { get; set; } = 200;

    [Range(0, 86400)]
    public int LookbackSeconds { get; set; } = 120;

    [MaxLength(64)]
    public string ClientSubnetPrefix { get; set; } = string.Empty;
}
