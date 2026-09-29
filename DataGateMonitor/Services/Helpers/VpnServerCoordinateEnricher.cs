using System.Net;
using DataGateMonitor.Models;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Interfaces;

namespace DataGateMonitor.Services.Helpers;

/// <summary>
/// Fills missing server map coordinates from GeoLite using an already-known IP.
/// Intentionally avoids DNS / node HTTP so Add/Update stay fast and never block on the network.
/// </summary>
public sealed class VpnServerCoordinateEnricher(
    IGeoLiteQueryService geoLiteQueryService,
    GeoLiteDatabaseFactory geoLiteDatabaseFactory,
    ILogger<VpnServerCoordinateEnricher> logger) : IVpnServerCoordinateEnricher
{
    public async Task TryFillMissingAsync(
        VpnServer server,
        string? preferredPublicIp,
        CancellationToken cancellationToken)
    {
        // Never let geo enrichment fail Add/Update — wrap everything.
        try
        {
            // Only when both coordinates are unset (do not overwrite manual values).
            if (server.Latitude is not null || server.Longitude is not null)
                return;

            if (!await geoLiteDatabaseFactory.TryEnsureReadyAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogDebug("Skipping VpnServer coordinate fill: GeoLite2 is not configured or not loaded.");
                return;
            }

            var ip = ResolveIpForGeo(server.ApiUrl, preferredPublicIp);
            if (string.IsNullOrWhiteSpace(ip))
                return;

            var geo = await geoLiteQueryService.GetGeoInfoAsync(ip, cancellationToken).ConfigureAwait(false);
            if (geo?.Latitude is not { } lat || geo.Longitude is not { } lon)
                return;

            // GeoLite maps RFC1918 / unknown placeholders to 0,0 — skip Null Island.
            if (lat == 0 && lon == 0)
                return;

            // Re-check in case something else filled coords concurrently (unlikely but safe).
            if (server.Latitude is not null || server.Longitude is not null)
                return;

            server.Latitude = lat;
            server.Longitude = lon;
            logger.LogInformation(
                "Filled VpnServer coordinates from GeoLite for {Ip}: {Lat}, {Lon}",
                ip, lat, lon);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "VpnServer coordinate enrichment failed; continuing without coordinates.");
        }
    }

    /// <summary>
    /// Preferred public IP, else literal IP host from ApiUrl. No DNS — hostnames are skipped.
    /// </summary>
    private static string? ResolveIpForGeo(string? apiUrl, string? preferredPublicIp)
    {
        var fromHint = VpnServerApiUrlHelper.TryParsePublicIp(preferredPublicIp);
        if (fromHint is not null)
            return fromHint;

        if (!VpnServerApiUrlHelper.TryGetEndpointHostPort(apiUrl, out var host, out _))
            return null;

        return VpnServerApiUrlHelper.TryParseHostIp(host);
    }
}
