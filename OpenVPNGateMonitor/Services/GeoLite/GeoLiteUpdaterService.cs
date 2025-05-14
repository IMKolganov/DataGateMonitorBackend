using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models.Helpers.Api;
using OpenVPNGateMonitor.Services.GeoLite.Helpers;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;
using OpenVPNGateMonitor.Services.Helpers;

namespace OpenVPNGateMonitor.Services.GeoLite;

public class GeoLiteUpdaterService(
    ILogger<GeoLiteUpdaterService> logger,
    HttpClient httpClient,
    GeoLiteDatabaseFactory databaseFactory,
    IServiceProvider serviceProvider)
    : IGeoLiteUpdaterService
{
    public async Task<GeoLiteUpdateResponse> DownloadAndUpdateDatabaseAsync(CancellationToken cancellationToken)
    {
        var result = new GeoLiteUpdateResponse();

        try
        {
            const int totalSteps = 8;

            // Step 1: Load configuration
            await ReportStepProgressAsync(1, totalSteps, "Load configuration", 0, cancellationToken);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var dbPath =
                await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Db_Path", serviceProvider,
                    cancellationToken);
            await ReportStepProgressAsync(1, totalSteps, "Load configuration", 50, cancellationToken);
            if (string.IsNullOrEmpty(dbPath))
                throw new InvalidOperationException("GeoIp_Db_Path is not configured.");
            await ReportStepProgressAsync(1, totalSteps, "Load configuration", 100, cancellationToken);

            // Step 2: Prepare directories
            await ReportStepProgressAsync(2, totalSteps, "Prepare directories", 0, cancellationToken);
            var baseDir = Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException("Invalid GeoIp_Db_Path");
            var extractDir = Path.Combine(baseDir, $"GeoLite2_{timestamp}");
            var tempFile = Path.Combine(extractDir, $"GeoLite2-City_{timestamp}.tar.gz");
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
            if (!Directory.Exists(extractDir)) Directory.CreateDirectory(extractDir);
            await ReportStepProgressAsync(2, totalSteps, "Prepare directories", 100, cancellationToken);

            // Step 3: Get download info
            await ReportStepProgressAsync(3, totalSteps, "Get download URL and credentials", 0, cancellationToken);
            var downloadUrl = await GetDownloadUrlAsync(cancellationToken);
            var credentials = await GetAuthHeaderAsync(cancellationToken);
            result.DownloadUrl = downloadUrl;
            result.TempFilePath = tempFile;
            await ReportStepProgressAsync(3, totalSteps, "Get download URL and credentials", 100, cancellationToken);

            // Step 4: Download file (has internal progress)
            logger.LogInformation("Downloading GeoLite2 database...");
            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response =
                await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = HandleHttpError(response);
                logger.LogError("Failed to download database: {ErrorMessage}", result.ErrorMessage);
                return result;
            }

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // CopyWithProgressAsync должен вызывать ReportStepProgressAsync(4, totalSteps, ..., percent)
                await CopyWithProgressAsync(stream, fileStream, totalBytes, 4, 8, "Download file", cancellationToken);
            }

            // Step 5: Extract archive
            await ReportStepProgressAsync(5, totalSteps, "Extract archive", 0, cancellationToken);
            await GZip.ExtractTarGzAsync(tempFile, extractDir,
                async percent => await ReportStepProgressAsync(5, totalSteps, "Extract archive", percent, cancellationToken),
                cancellationToken);
            await ReportStepProgressAsync(5, totalSteps, "Extract archive", 100, cancellationToken);

            // Step 6: Find and validate .mmdb
            await ReportStepProgressAsync(6, totalSteps, "Find .mmdb file", 0, cancellationToken);
            var extractedDirs = Directory.GetDirectories(extractDir);
            if (extractedDirs.Length == 0)
            {
                result.ErrorMessage = "Extraction failed: No directories found.";
                logger.LogError(result.ErrorMessage);
                return result;
            }

            var extractedPath = extractedDirs.First();
            result.ExtractedPath = extractedPath;

            var mmdbFile = Directory.GetFiles(extractedPath, "*.mmdb", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (mmdbFile == null)
            {
                result.ErrorMessage = "Database file not found after extraction.";
                logger.LogError(result.ErrorMessage);
                return result;
            }

            await ReportStepProgressAsync(6, totalSteps, "Find .mmdb file", 100, cancellationToken);

            // Step 7: Replace current database
            await ReportStepProgressAsync(7, totalSteps, "Replace current database", 0, cancellationToken);
            result.DatabasePath = dbPath;
            if (result.DatabasePath == databaseFactory.DatabasePath)
                await databaseFactory.DeleteDatabaseAsync(cancellationToken);

            File.Copy(mmdbFile, dbPath, true);
            result.Success = true;
            await ReportStepProgressAsync(7, totalSteps, "Replace current database", 100, cancellationToken);

            // Step 8: Reload in-memory database
            await ReportStepProgressAsync(8, totalSteps, "Reload in-memory database", 0, cancellationToken);
            await databaseFactory.ReloadDatabaseAsync(cancellationToken);
            await ReportStepProgressAsync(8, totalSteps, "Reload in-memory database", 100, cancellationToken);

            logger.LogInformation("GeoLite2 database update completed successfully.");
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            logger.LogError(ex, "Error updating GeoLite2 database.");
        }

        return result;
    }

    private async Task<string> GetDownloadUrlAsync(CancellationToken cancellationToken)
    {
        return await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Download_Url", serviceProvider,
                   cancellationToken)
               ?? throw new InvalidOperationException("GeoIp_Download_Url is not configured.");
    }

    private async Task<string> GetAuthHeaderAsync(CancellationToken cancellationToken)
    {
        var accountId =
            await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Account_ID", serviceProvider,
                cancellationToken);
        var licenseKey =
            await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_License_Key", serviceProvider,
                cancellationToken);

        if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(licenseKey))
            throw new InvalidOperationException("GeoLite Account ID or License Key is missing.");

        return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountId}:{licenseKey}"));
    }

    private string HandleHttpError(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var reason = response.ReasonPhrase ?? "Unknown";

        var errorMessage = statusCode switch
        {
            400 => "400 Bad Request – Invalid request sent to the server.",
            401 => "401 Unauthorized – Invalid API key or authentication failed.",
            403 => "403 Forbidden – Access to the resource is restricted.",
            404 => "404 Not Found – The requested resource could not be found.",
            429 => "429 Too Many Requests – Rate limit exceeded, try again later.",
            500 => "500 Internal Server Error – The server encountered an error.",
            503 => "503 Service Unavailable – The service is temporarily down, retry later.",
            _ => $"{statusCode} {reason} – Unexpected error occurred."
        };

        logger.LogError("Failed to retrieve database version: {ErrorMessage}", errorMessage);
        return $"Error: {errorMessage}";
    }

    private async Task CopyWithProgressAsync(Stream stream, FileStream fileStream, long totalBytes, 
        int currentStep, int totalSteps, string stepTitle, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;
        int? lastPercentSent = null;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                var percent = (int)(totalRead * 100 / totalBytes);

                if (lastPercentSent != percent)
                {
                    lastPercentSent = percent;

                    await ReportStepProgressAsync(currentStep, totalSteps, stepTitle, percent, cancellationToken);
                }
            }
        }

        // Убедимся, что 100% отправлено в конце
        if (lastPercentSent != 100)
        {
            await ReportStepProgressAsync(currentStep, totalSteps, stepTitle, 100, cancellationToken);
        }
    }
    
    private async Task ReportStepProgressAsync(int step, int totalSteps, string title, int progress, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GeoLiteHub>>();

        await hubContext.Clients.All.SendAsync("GeoLiteStepProgress", new
        {
            step,
            totalSteps,
            title,
            progress // 0–100 for the current step
        }, cancellationToken);
    }
}