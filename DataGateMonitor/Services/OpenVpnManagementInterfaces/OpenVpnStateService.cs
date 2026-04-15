using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces;

public class OpenVpnStateService(ILogger<IOpenVpnStateService> logger, 
    IOpenVpnMicroserviceClientFactory openVpnMicroserviceClientFactory) : IOpenVpnStateService
{
    public async Task<OpenVpnState> GetStateAsync(VpnServer openVpnServer, CancellationToken cancellationToken)
    {
        var client = openVpnMicroserviceClientFactory.Create(openVpnServer);
        var response = await client.SendCommandWithResponseAsync("state", cancellationToken);

        logger.LogDebug("Received status response:\n{Response}", response);

        return ParseState(response);
    }
    
    private OpenVpnState ParseState(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            logger.LogWarning("[STATE PARSER] Received empty response from OpenVPN.");
            return new OpenVpnState { Success = false };
        }

        var lines = data.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        OpenVpnState state = new();

        try
        {
            foreach (var line in lines)
            {
                var parts = line.Split(",");
                if (parts.Length < 5)
                {
                    logger.LogWarning($"[STATE PARSER] Skipping malformed line: {line}");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(parts[0]))
                {
                    if (long.TryParse(parts[0], out long timestamp))
                    {
                        state.UpSince = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                    }
                    else
                    {
                        logger.LogError($"[STATE PARSER] Invalid date format: {parts[0]}");
                        throw new Exception($"Invalid date: {parts[0]}");
                    }
                }

                state.Connected = parts[1] == "CONNECTED";
                state.Success = parts[2] == "SUCCESS";
                state.ServerLocalIp = parts[3];
                state.ServerRemoteIp = parts[4];
            }

            return state;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[STATE PARSER] Error while parsing state data: {data}");
            throw;
        }
    }
}