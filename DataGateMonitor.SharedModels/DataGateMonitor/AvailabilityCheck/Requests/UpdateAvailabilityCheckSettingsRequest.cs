namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Requests;

public sealed class UpdateAvailabilityCheckSettingsRequest
{
    public bool Enabled { get; set; } = true;

    /// <summary>URL that the external probe should check (passed as <c>target</c> query param).</summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// Probe endpoint, e.g. <c>https://status.rackot.ru/check.cgi</c>.
    /// Must accept <c>?target=</c> and return <see cref="Dto.AvailabilityProbeResultDto"/> JSON.
    /// </summary>
    public string ProbeUrl { get; set; } = string.Empty;
}
