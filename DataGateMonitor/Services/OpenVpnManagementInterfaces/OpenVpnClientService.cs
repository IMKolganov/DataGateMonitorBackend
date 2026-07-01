using System.Globalization;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces;

public class OpenVpnClientService(
    ILogger<IOpenVpnClientService> logger,
    IOpenVpnMicroserviceClientFactory openVpnMicroserviceClientFactory,
    IProxyClientLookupService proxyClientLookupService)
    : IOpenVpnClientService
{
    public async Task<OpenVpnManagementStatusResult> GetClientsFromManagementAsync(VpnServer openVpnServer,
        CancellationToken cancellationToken)
    {
        var client = openVpnMicroserviceClientFactory.Create(openVpnServer);
        var response = await client.SendCommandWithResponseAsync("status 3", cancellationToken);

        logger.LogDebug("Received status response:\n{Response}", response);

        var result = await ParseStatus(response, openVpnServer, cancellationToken);
        logger.LogInformation("Found {ClientCount} connected clients", result.Clients.Count);

        if (result.Clients.Any())
        {
            logger.LogDebug("Connected clients: {Clients}",
                string.Join(", ", result.Clients.Select(c => c.CommonName)));
        }

        return result;
    }

    private async Task<OpenVpnManagementStatusResult> ParseStatus(string data, VpnServer server,
        CancellationToken cancellationToken)
    {
        var result = new OpenVpnManagementStatusResult();
        var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("GLOBAL_STATS", StringComparison.Ordinal))
            {
                TryParseDcoEnabled(line, result);
                continue;
            }

            if (!line.StartsWith("CLIENT_LIST", StringComparison.Ordinal)) continue;

            var parts = line.Split('\t');
            if (parts.Length < 8) continue; // defensive: ensure required columns exist

            var client = TryParseClient(parts);
            if (client is null) continue;

            await proxyClientLookupService.EnrichFromManagementRealAddressAsync(server, client, cancellationToken);

            client.SessionId = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince(
                client.CommonName, client.RemoteIp, client.ConnectedSince);
            result.Clients.Add(client);
        }

        return result;
    }

    /// <summary>Parse GLOBAL_STATS line: "GLOBAL_STATS\tdco_enabled\t0" or "\t1".</summary>
    private static void TryParseDcoEnabled(string line, OpenVpnManagementStatusResult result)
    {
        var parts = line.Split('\t');
        if (parts.Length < 3) return;
        if (!string.Equals(parts[1], "dco_enabled", StringComparison.OrdinalIgnoreCase)) return;
        if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            result.DcoEnabled = value != 0;
    }

    private VpnServerClient? TryParseClient(string[] parts)
    {
        try
        {
            // Columns (status 3) can vary by version; the indexes below match the common format:
            // 0: CLIENT_LIST, 1: CommonName, 2: RealAddress, 3: VirtualAddress, 4: Virtual6 (optional),
            // 5: BytesReceived, 6: BytesSent, 7: ConnectedSince (unix), ... , 9: Username (may be UNDEF)
            var remoteIp = OpenVpnRealAddressParser.NormalizeRemoteIp(parts[2]);

            return new VpnServerClient
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
}