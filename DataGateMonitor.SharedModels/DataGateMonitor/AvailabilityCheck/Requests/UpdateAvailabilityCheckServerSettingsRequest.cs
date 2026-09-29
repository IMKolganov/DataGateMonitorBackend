namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Requests;

/// <summary>Enable/disable the external availability probe for one VPN server.</summary>
public sealed class UpdateAvailabilityCheckServerSettingsRequest
{
    /// <summary>
    /// When false, this server is skipped by the background probe and is not blocked by
    /// <c>IsAvailableByExternalProbe</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
