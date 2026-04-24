using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Dto;

namespace DataGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteQueryService
{
    Task<OpenVpnGeoInfo?> GetGeoInfoAsync(string ip, CancellationToken cancellationToken);
    Task<string> GetDatabaseVersionAsync(CancellationToken cancellationToken);
    string GetDatabasePath();
}