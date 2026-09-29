namespace DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Requests;

public sealed class UpdateAvailabilityCheckSettingsRequest
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Probe endpoint, e.g. <c>https://status.rackot.ru/check.cgi</c>.
    /// Must accept <c>?target=</c> and return <see cref="Dto.AvailabilityProbeResultDto"/> JSON.
    /// Each VPN server is probed using its <c>ApiUrl</c> as the target.
    /// </summary>
    public string ProbeUrl { get; set; } = string.Empty;
}
