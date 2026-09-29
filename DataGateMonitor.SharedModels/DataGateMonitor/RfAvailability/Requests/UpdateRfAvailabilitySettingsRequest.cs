namespace DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Requests;

public sealed class UpdateRfAvailabilitySettingsRequest
{
    public bool Enabled { get; set; } = true;

    /// <summary>Target URL probed from RF via status.rackot.ru (e.g. https://host:9443/).</summary>
    public string TargetUrl { get; set; } = string.Empty;
}
