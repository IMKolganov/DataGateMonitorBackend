namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;

public sealed class VpnServerPiHoleRuntimeConfigResponse
{
    public int VpnServerId { get; set; }

    public bool IsPiHoleEnabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string AppPassword { get; set; } = string.Empty;

    public int PollIntervalSeconds { get; set; }

    public int BatchSize { get; set; }

    public int LookbackSeconds { get; set; }

    public string ClientSubnetPrefix { get; set; } = string.Empty;
}
