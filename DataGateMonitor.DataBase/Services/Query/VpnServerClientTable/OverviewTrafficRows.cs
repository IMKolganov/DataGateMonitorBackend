namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public sealed class OverviewTrafficBucketRow
{
    public DateTimeOffset BucketTs { get; set; }
    public int ActiveClients { get; set; }
    public long TrafficInBytes { get; set; }
    public long TrafficOutBytes { get; set; }
}

public sealed class OverviewUsersSeriesBucketRow
{
    public DateTimeOffset BucketTs { get; set; }
    public int ActiveSessions { get; set; }
    public int ActiveUsers { get; set; }
}

public sealed class OverviewUserTrafficRow
{
    public string ExternalId { get; set; } = "";
    public int? VpnServerId { get; set; }
    public int Sessions { get; set; }
    public long TrafficInBytes { get; set; }
    public long TrafficOutBytes { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
}

public sealed class OverviewTrafficTotalsRow
{
    public long TrafficInBytes { get; set; }
    public long TrafficOutBytes { get; set; }
}
