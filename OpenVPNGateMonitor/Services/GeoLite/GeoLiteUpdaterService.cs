using System.Net;
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
                var errorMessage = httpErrorMapper.Map(response);
                logger.LogError("Failed to download database: {ErrorMessage}", errorMessage);
                throw new Exception($"Failed to download database: {errorMessage}");
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
                throw new Exception("Extraction failed: No directories found.");

            var extractedPath = extractedDirs.First();
            result.ExtractedPath = extractedPath;

            var mmdbFile = Directory.GetFiles(extractedPath, "*.mmdb", SearchOption.AllDirectories).FirstOrDefault();
            if (mmdbFile is null)
                throw new Exception("Extraction failed: No .mmdb file found.");

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
            logger.LogError(ex, "Error updating GeoLite2 database.{Error}", ex.Message);
            throw;
        }
    }
    
    public async Task<GeoLiteVersionCheckResponse> CheckNewVersionAsync(CancellationToken cancellationToken)
    {
        var resp = new GeoLiteVersionCheckResponse();

        try
        {
            // 1) Resolve local db path and local file info
            var dbPath = await config.GetDatabasePathAsync(cancellationToken);
            if (File.Exists(dbPath))
            {
                var fi = new FileInfo(dbPath);
                resp.LocalLastWriteTimeUtc = fi.LastWriteTimeUtc;
                resp.LocalFileSize = fi.Length;
            }

            // 2) Prepare remote request (prefer HEAD)
            var url = await auth.GetDownloadUrlAsync(cancellationToken);
            var basic = await auth.GetBasicAuthHeaderAsync(cancellationToken);
            resp.CheckedUrl = url;

            // Try HEAD first
            var head = new HttpRequestMessage(HttpMethod.Head, url);
            head.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            using var headResponse = await httpClient.SendAsync(
                head, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            HttpResponseMessage headersResponse = headResponse;

            // 3) Fallback to GET headers-only if HEAD not allowed
            if (headResponse.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound)
            {
                var get = new HttpRequestMessage(HttpMethod.Get, url);
                get.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

                var getResponse = await httpClient.SendAsync(
                    get, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                headersResponse = getResponse;
            }

            if (!headersResponse.IsSuccessStatusCode)
            {
                var errorMessage = httpErrorMapper.Map(headersResponse);
                logger.LogWarning("GeoLite version check failed: {Error}", errorMessage);
                throw new Exception($"GeoLite version check failed:{errorMessage}");
            }

            // 4) Extract remote headers
            // Last-Modified
            if (headersResponse.Content?.Headers?.LastModified is not null)
                resp.RemoteLastModified = headersResponse.Content.Headers.LastModified;

            // ETag
            if (headersResponse.Headers.ETag is not null)
                resp.RemoteETag = headersResponse.Headers.ETag.Tag?.Trim('"') ?? throw new InvalidOperationException();

            // Content-Length
            if (headersResponse.Content?.Headers?.ContentLength is not null)
                resp.RemoteContentLength = headersResponse.Content.Headers.ContentLength;

            // 5) Decide if update is available
            // Priority: Last-Modified > Content-Length > (fallback) Local not found
            bool shouldUpdate = false;

            if (resp.LocalLastWriteTimeUtc is null)
            {
                // No local file
                shouldUpdate = true;
            }
            else if (resp.RemoteLastModified is not null)
            {
                // Compare remote "Last-Modified" to local write time
                // Small skew tolerance of 1 minute
                var tolerance = TimeSpan.FromMinutes(1);
                shouldUpdate = resp.RemoteLastModified.Value.UtcDateTime - resp.LocalLastWriteTimeUtc.Value > tolerance;
            }
            else if (resp.RemoteContentLength is not null && resp.LocalFileSize is not null)
            {
                // If server doesn't provide Last-Modified, use size mismatch as heuristic
                shouldUpdate = resp.RemoteContentLength.Value != resp.LocalFileSize.Value;
            }

            resp.IsUpdateAvailable = shouldUpdate;

            return resp;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking GeoLite remote version. {ErrorMessage}", ex.Message);
            throw new Exception($"Error while checking GeoLite remote version. {ex.Message}");
        }
    }
}