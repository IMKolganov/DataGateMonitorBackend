using System.Net;
using MaxMind.GeoIP2.Exceptions;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Services.OpenVpnManagementInterfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Dto;

namespace DataGateMonitor.Services.GeoLite;

public class GeoLiteQueryService(GeoLiteDatabaseFactory dbFactory, ILogger<GeoLiteQueryService> logger)
    : IGeoLiteQueryService
{
    public string GetDatabasePath()
    {
        return dbFactory.GetDatabasePath();
    }
    
    public async Task<string> GetDatabaseVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var databaseReader = await dbFactory.GetDatabaseAsync(cancellationToken);
            var metadata = databaseReader.Metadata;

            var version = metadata.BuildDate.ToString("yyyy-MM-dd HH:mm:ss");

            logger.LogInformation("GeoLite2 database version (Build Date): {Version}", version);
            return version;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving database version. Error: {Error}", ex.Message);
            return $"Error retrieving version. {ex.Message}";
        }
    }
    
    public async Task<OpenVpnGeoInfo?> GetGeoInfoAsync(string ip, CancellationToken cancellationToken)
    {
        try
        {
            ip = ExtractIpHost(ip);
            if (string.IsNullOrWhiteSpace(ip))
                return null;

            var ipAddress = IPAddress.Parse(ip);

            if (IsPrivateIp(ipAddress))
            {
                return new OpenVpnGeoInfo
                {
                    Country = "Internet",
                    Region = "RFC1918",
                    City = "RFC1918",
                    Latitude = 0,
                    Longitude = 0
                };
            }

            if (ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6Multicast)
                return null;

            var databaseReader = await dbFactory.GetDatabaseAsync(cancellationToken);
            var cityResponse = databaseReader.City(ip);

            return new OpenVpnGeoInfo
            {
                Country = cityResponse.RegisteredCountry.IsoCode ?? cityResponse.Country.IsoCode,
                Region = cityResponse.MostSpecificSubdivision.IsoCode
                         ?? cityResponse.Subdivisions.LastOrDefault()?.IsoCode
                         ?? cityResponse.RegisteredCountry.IsoCode,
                City = cityResponse.City.Name
                       ?? cityResponse.Location.TimeZone
                       ?? cityResponse.RegisteredCountry.Name,
                Latitude = cityResponse.Location.Latitude,
                Longitude = cityResponse.Location.Longitude
            };
        }
        catch (AddressNotFoundException ex)
        {
            logger.LogWarning($"GeoIP not found for {ip}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error getting GeoIP for {ip} Error: {ex.Message}");
            return null;
        }
    }
    // Extract host from management RealAddress / endpoint strings (legacy and OpenVPN 2.7+).
    private static string ExtractIpHost(string realAddress)
    {
        if (string.IsNullOrWhiteSpace(realAddress))
            return realAddress;

        if (OpenVpnRealAddressParser.TryParseHostPort(realAddress, out var host, out _))
            return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : host;

        var s = realAddress.Trim();

        // [IPv6]:port
        if (s.Length > 2 && s[0] == '[')
        {
            var rb = s.IndexOf(']');
            if (rb > 0) return s.Substring(1, rb - 1);
        }

        // IPv4:port or unbracketed host:port (take before last ':')
        var idx = s.LastIndexOf(':');
        if (idx > 0) return s[..idx];

        // Bare IPv6 or host without port
        return s;
    }
    
    private bool IsPrivateIp(IPAddress ip)
    {
        byte[] bytes = ip.GetAddressBytes();
        return (bytes[0] == 10) ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168) ||
               IPAddress.IsLoopback(ip);
    }
}