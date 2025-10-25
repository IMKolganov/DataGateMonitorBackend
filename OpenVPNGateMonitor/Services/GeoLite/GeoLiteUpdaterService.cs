using System.Net.Http.Headers;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;
using OpenVPNGateMonitor.Services.Helpers;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Responses;

namespace OpenVPNGateMonitor.Services.GeoLite;

public class GeoLiteUpdaterService(
    ILogger<GeoLiteUpdaterService> logger,
    HttpClient httpClient,
    GeoLiteDatabaseFactory databaseFactory,
    IGeoLiteConfigProvider config,
    IGeoLiteAuthProvider auth,
    IGeoLiteProgressNotifier progress,
    IHttpErrorMapper httpErrorMapper,
    IStreamCopier streamCopier)
    : IGeoLiteUpdaterService
{
    public async Task<GeoLiteUpdateResponse> DownloadAndUpdateDatabaseAsync(CancellationToken cancellationToken)
    {
        var result = new GeoLiteUpdateResponse();
        const int totalSteps = 8;

        try
        {
            // Step 1: Load configuration
            await progress.ReportStepAsync(1, totalSteps, "Load configuration", 0, cancellationToken);
            var timestamp = config.CreateTimestamp();
            var dbPath = await config.GetDatabasePathAsync(cancellationToken);
            await progress.ReportStepAsync(1, totalSteps, "Load configuration", 100, cancellationToken);

            // Step 2: Prepare directories
            await progress.ReportStepAsync(2, totalSteps, "Prepare directories", 0, cancellationToken);
            var (_, extractDir, tempFile) = config.PreparePaths(dbPath, timestamp);
            await progress.ReportStepAsync(2, totalSteps, "Prepare directories", 100, cancellationToken);

            // Step 3: Get download info
            await progress.ReportStepAsync(3, totalSteps, "Get download URL and credentials", 0, cancellationToken);
            var downloadUrl = await auth.GetDownloadUrlAsync(cancellationToken);
            var credentials = await auth.GetBasicAuthHeaderAsync(cancellationToken);
            result.DownloadUrl = downloadUrl;
            result.TempFilePath = tempFile;
            await progress.ReportStepAsync(3, totalSteps, "Get download URL and credentials", 100, cancellationToken);

            // Step 4: Download the file (with internal progress)
            logger.LogInformation("Downloading GeoLite2 database...");
            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = httpErrorMapper.Map(response);
                logger.LogError("Failed to download database: {ErrorMessage}", result.ErrorMessage);
                return result;
            }

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await streamCopier.CopyWithProgressAsync(
                    networkStream, fileStream, totalBytes, currentStep: 4, totalSteps, "Download file", cancellationToken);
            }

            // Step 5: Extract archive
            await progress.ReportStepAsync(5, totalSteps, "Extract archive", 0, cancellationToken);
            await GZip.ExtractTarGzAsync(
                tempFile, extractDir,
                percent => progress.ReportStepAsync(5, totalSteps, "Extract archive", percent, cancellationToken),
                cancellationToken);
            await progress.ReportStepAsync(5, totalSteps, "Extract archive", 100, cancellationToken);

            // Step 6: Find .mmdb
            await progress.ReportStepAsync(6, totalSteps, "Find .mmdb file", 0, cancellationToken);
            var extractedDirs = Directory.GetDirectories(extractDir);
            if (extractedDirs.Length == 0)
                return Fail("Extraction failed: No directories found.");

            var extractedPath = extractedDirs.First();
            result.ExtractedPath = extractedPath;

            var mmdbFile = Directory.GetFiles(extractedPath, "*.mmdb", SearchOption.AllDirectories).FirstOrDefault();
            if (mmdbFile is null)
                return Fail("Database file not found after extraction.");

            await progress.ReportStepAsync(6, totalSteps, "Find .mmdb file", 100, cancellationToken);

            // Step 7: Replace current database
            await progress.ReportStepAsync(7, totalSteps, "Replace current database", 0, cancellationToken);
            result.DatabasePath = dbPath;
            if (result.DatabasePath == databaseFactory.DatabasePath)
                await databaseFactory.DeleteDatabaseAsync(cancellationToken);

            File.Copy(mmdbFile, dbPath, overwrite: true);
            result.Success = true;
            await progress.ReportStepAsync(7, totalSteps, "Replace current database", 100, cancellationToken);

            // Step 8: Reload the in-memory database
            await progress.ReportStepAsync(8, totalSteps, "Reload in-memory database", 0, cancellationToken);
            await databaseFactory.ReloadDatabaseAsync(cancellationToken);
            await progress.ReportStepAsync(8, totalSteps, "Reload in-memory database", 100, cancellationToken);

            if (result.Success)
                await progress.NotifyFinishedAsync(cancellationToken);

            logger.LogInformation("GeoLite2 database update completed successfully.");
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            logger.LogError(ex, "Error updating GeoLite2 database.");
            return result;
        }

        GeoLiteUpdateResponse Fail(string msg)
        {
            var r = new GeoLiteUpdateResponse { ErrorMessage = msg };
            logger.LogError(msg);
            return r;
        }
    }
}