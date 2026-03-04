using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Responses;

namespace OpenVPNGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteUpdaterService
{
    Task<GeoLiteUpdateResponse> DownloadAndUpdateDatabaseAsync(CancellationToken cancellationToken);
    Task<GeoLiteVersionCheckResponse> CheckNewVersionAsync(CancellationToken cancellationToken);
}