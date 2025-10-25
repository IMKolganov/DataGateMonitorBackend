using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Dto;

namespace OpenVPNGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteQueryService
{
    Task<OpenVpnGeoInfo?> GetGeoInfoAsync(string ip, CancellationToken cancellationToken);
    Task<string> GetDatabaseVersionAsync(CancellationToken cancellationToken);
    string GetDatabasePath();
}