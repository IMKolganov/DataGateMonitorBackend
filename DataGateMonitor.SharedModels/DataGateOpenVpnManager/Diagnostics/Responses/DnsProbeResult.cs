namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

/// <summary>
/// Result of a UDP DNS probe performed during proxy session diagnostics.
/// </summary>
public sealed class DnsProbeResult
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public bool Responded { get; set; }

    public int ResponseBytes { get; set; }

    public string? Error { get; set; }
}
