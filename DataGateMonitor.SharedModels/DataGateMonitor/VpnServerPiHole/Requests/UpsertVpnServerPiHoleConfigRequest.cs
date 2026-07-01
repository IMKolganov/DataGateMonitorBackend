using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;

public sealed class UpsertVpnServerPiHoleConfigRequest
{
    [Range(1, int.MaxValue)]
    public int VpnServerId { get; set; }

    [Required]
    public string BaseUrl { get; set; } = "http://127.0.0.1:8080";

    /// <summary>Leave empty on update to keep the existing password.</summary>
    public string? AppPassword { get; set; }

    [Range(10, 3600)]
    public int PollIntervalSeconds { get; set; } = 60;

    [Range(1, 10000)]
    public int BatchSize { get; set; } = 200;

    [Range(0, 3600)]
    public int LookbackSeconds { get; set; } = 120;

    public string ClientSubnetPrefix { get; set; } = string.Empty;
}
