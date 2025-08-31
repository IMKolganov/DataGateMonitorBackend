namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

public sealed class TotalsPayload
{
    public long SessionsCount { get; set; }
    public long UsersCount { get; set; }
    public long TrafficInBytes { get; set; }
    public long TrafficOutBytes { get; set; }
    public long TrafficTotalBytes => TrafficInBytes + TrafficOutBytes;
}