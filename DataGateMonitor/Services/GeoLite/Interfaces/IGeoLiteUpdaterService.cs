using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Responses;

namespace DataGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteUpdaterService
{
    Task<GeoLiteUpdateResponse> DownloadAndUpdateDatabaseAsync(CancellationToken cancellationToken);
    Task<GeoLiteVersionCheckResponse> CheckNewVersionAsync(CancellationToken cancellationToken);
}