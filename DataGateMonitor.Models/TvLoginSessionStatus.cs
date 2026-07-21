namespace DataGateMonitor.Models;

/// <summary>Lifecycle of a Netflix-style TV / device-linking login session.</summary>
public enum TvLoginSessionStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Expired = 3,
    Consumed = 4,
}
