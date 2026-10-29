namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.PiHole.Dto;

public sealed class PiHoleOptionsDto
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string AppPassword { get; set; } = string.Empty;

    public bool HasAppPassword { get; set; }

    public int PollIntervalSeconds { get; set; }

    public int BatchSize { get; set; }

    public int LookbackSeconds { get; set; }

    public string ClientSubnetPrefix { get; set; } = string.Empty;
}
