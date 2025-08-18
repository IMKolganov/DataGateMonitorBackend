using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces;

public class OpenVpnClientService(
    ILogger<IOpenVpnClientService> logger,
    IOpenVpnMicroserviceClientFactory openVpnMicroserviceClientFactory,
    IGeoLiteQueryService geoLiteQueryService)
    : IOpenVpnClientService
{
    public async Task<List<OpenVpnServerClient>> GetClientsFromManagementAsync(OpenVpnServer openVpnServer, 
        CancellationToken cancellationToken)
    {
        var client = openVpnMicroserviceClientFactory.Create(openVpnServer);
        var response = await client.SendCommandWithResponseAsync("status 3", cancellationToken);

        logger.LogDebug("Received status response:\n{Response}", response);

        var clients = await ParseStatus(response, cancellationToken);
        logger.LogInformation("Found {ClientCount} connected clients", clients.Count);

        if (clients.Any())
        {
            logger.LogDebug("Connected clients: {Clients}",
                string.Join(", ", clients.Select(c => c.CommonName)));
        }

        return clients;
    }

    private async Task<List<OpenVpnServerClient>> ParseStatus(string data, CancellationToken cancellationToken)
    {
        var clients = new List<OpenVpnServerClient>();
        var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (!line.StartsWith("CLIENT_LIST", StringComparison.Ordinal)) continue;

            var parts = line.Split('\t');
            if (parts.Length < 8) continue; // defensive: ensure required columns exist

            var client = TryParseClient(parts);
            if (client is null) continue;

            var geoInfo = await geoLiteQueryService.GetGeoInfoAsync(client.RemoteIp, cancellationToken);
            if (geoInfo is not null)
            {
                client.Country = geoInfo.Country;
                client.Region = geoInfo.Region;
                client.City = geoInfo.City;
                client.Latitude = geoInfo.Latitude;
                client.Longitude = geoInfo.Longitude;
            }

            client.SessionId = GenerateSessionId(client.CommonName, client.RemoteIp, client.ConnectedSince);
            clients.Add(client);
        }

        return clients;
    }

    private OpenVpnServerClient? TryParseClient(string[] parts)
    {
        try
        {
            // Columns (status 3) can vary by version; the indexes below match the common format:
            // 0: CLIENT_LIST, 1: CommonName, 2: RealAddress, 3: VirtualAddress, 4: Virtual6 (optional),
            // 5: BytesReceived, 6: BytesSent, 7: ConnectedSince (unix), ... , 9: Username (may be UNDEF)
            var realAddress = parts[2];
            var remoteIp = realAddress;// ExtractIpHost(realAddress);

            return new OpenVpnServerClient
            {
                CommonName = parts[1],
                RemoteIp = remoteIp,
                LocalIp = parts[3],
                BytesReceived = TryParseLong(parts[5], "BytesReceived"),
                BytesSent = TryParseLong(parts[6], "BytesSent"),
                ConnectedSince = TryParseInstantUtc(parts[7], "ConnectedSince"),
                Username = parts.Length > 9 && parts[9] != "UNDEF" ? parts[9] : parts[1]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing OpenVPN client data. Raw parts: {Parts}", string.Join("|", parts));
            return null;
        }
    }

    private long TryParseLong(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            logger.LogWarning("{FieldName} is empty. Using default value 0.", fieldName);
            return 0;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return result;

        logger.LogError("Failed to parse {FieldName}. Value: '{Value}'", fieldName, value);
        throw new FormatException($"Invalid long format in field {fieldName}: '{value}'");
    }

    // Parse instant as UTC. Supports Unix seconds and ISO-8601 with or without offset.
    private DateTimeOffset TryParseInstantUtc(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            logger.LogWarning("{FieldName} is empty. Using DateTimeOffset.MinValue.", fieldName);
            return DateTimeOffset.MinValue;
        }

        // Unix seconds
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(unix); // already UTC
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse {FieldName} as Unix seconds. Value: '{Value}'", fieldName, value);
            }
        }

        // ISO-8601 / RFC3339
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto; // normalized to UTC
        }

        logger.LogError("Failed to parse {FieldName}. Value: '{Value}'", fieldName, value);
        throw new FormatException($"Invalid instant format in field {fieldName}: '{value}'");
    }

    // Extract host from "real address" handling IPv4, [IPv6]:port, and bare IPv6.
    private static string ExtractIpHost(string realAddress)
    {
        if (string.IsNullOrWhiteSpace(realAddress)) return realAddress;

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

    private Guid GenerateSessionId(string commonName, string realAddress, DateTimeOffset connectedSince)
    {
        var sessionString = $"{commonName}-{realAddress}-{connectedSince:o}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionString));
        return new Guid(hashBytes.Take(16).ToArray());
    }
}
