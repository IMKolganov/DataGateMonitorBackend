namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Dto;

public sealed class VpnServerPiHoleConfigDto
{
    public int VpnServerId { get; set; }

    public string BaseUrl { get; set; } = "http://127.0.0.1:8080";

    /// <summary>Masked in GET responses when a password is stored (e.g. "********").</summary>
    public string AppPassword { get; set; } = string.Empty;

    public bool HasAppPassword { get; set; }

    public int PollIntervalSeconds { get; set; } = 60;

    public int BatchSize { get; set; } = 200;

    public int LookbackSeconds { get; set; } = 120;

    public string ClientSubnetPrefix { get; set; } = string.Empty;
}
