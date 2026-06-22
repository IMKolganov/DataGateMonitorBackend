namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

public sealed class PiHoleDiagnosticsResponse
{
    public DateTime CheckedAtUtc { get; set; }

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public bool Authenticated { get; set; }

    public string? Error { get; set; }

    public int SampleQueryCount { get; set; }
}
