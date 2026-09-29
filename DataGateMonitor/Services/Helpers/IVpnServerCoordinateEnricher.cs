using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Helpers;

/// <summary>
/// Fills missing <see cref="VpnServer.Latitude"/> / <see cref="VpnServer.Longitude"/> from GeoLite using a public IP.
/// </summary>
public interface IVpnServerCoordinateEnricher
{
    /// <summary>
    /// When both coordinates are unset, resolve an IP (preferred hint, ApiUrl host, or DNS) and look up GeoLite.
    /// Lookup failures are ignored so save still succeeds.
    /// </summary>
    Task TryFillMissingAsync(VpnServer server, string? preferredPublicIp, CancellationToken cancellationToken);
}
