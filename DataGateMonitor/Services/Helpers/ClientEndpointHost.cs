using System.Net;

namespace DataGateMonitor.Services.Helpers;

/// <summary>Parses host/IP from a client endpoint string for GeoIP lookup.</summary>
public static class ClientEndpointHost
{
    /// <summary>
    /// Supports <c>ipv4:port</c>, <c>[ipv6]:port</c>, plain IPv4/hostname, or IPv6 without port.
    /// </summary>
    public static string? TryGetHostForGeoLookup(string? remoteAddress)
    {
        if (string.IsNullOrWhiteSpace(remoteAddress))
            return null;

        var s = remoteAddress.Trim();

        if (s.StartsWith('['))
        {
            var end = s.IndexOf(']', StringComparison.Ordinal);
            if (end > 1)
                return s[1..end];
            return null;
        }

        var colonCount = s.Count(c => c == ':');
        if (colonCount == 1)
        {
            var idx = s.IndexOf(':');
            var host = s[..idx];
            if (host.Length > 0 && !host.Contains(':'))
                return host;
        }

        return s;
    }

    public static bool IsNonLoopbackIpOrHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (!IPAddress.TryParse(host, out var ip))
            return true;

        return !IPAddress.IsLoopback(ip);
    }
}
