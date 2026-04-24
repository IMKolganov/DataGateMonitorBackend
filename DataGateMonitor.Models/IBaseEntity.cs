namespace DataGateMonitor.Models;

public interface IBaseEntity
{
    DateTimeOffset CreateDate { get; set; }
    DateTimeOffset LastUpdate { get; set; }
}