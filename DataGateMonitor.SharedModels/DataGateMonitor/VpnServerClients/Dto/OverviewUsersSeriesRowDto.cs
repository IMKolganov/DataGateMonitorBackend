namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

/// <summary>
/// One time bucket for users/sessions series: sessions, unique ExternalId (users), and unique CommonName (devices).
/// </summary>
public sealed class OverviewUsersSeriesRowDto
{
    public DateTimeOffset Ts { get; set; }
    public int ActiveSessions { get; set; }
    /// <summary>Distinct ExternalId (Google/account id) in the bucket.</summary>
    public int ActiveUsers { get; set; }
    /// <summary>Distinct CommonName (installation / device CN) in the bucket.</summary>
    public int ActiveDevices { get; set; }
}
