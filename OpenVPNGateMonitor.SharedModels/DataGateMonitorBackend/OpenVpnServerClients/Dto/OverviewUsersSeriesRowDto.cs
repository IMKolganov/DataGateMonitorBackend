namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

/// <summary>
/// One time bucket for users/sessions series: count of sessions and unique users in that bucket.
/// </summary>
public sealed class OverviewUsersSeriesRowDto
{
    public DateTimeOffset Ts { get; set; }
    public int ActiveSessions { get; set; }
    public int ActiveUsers { get; set; }
}
