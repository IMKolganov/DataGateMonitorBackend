using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces;

public class OpenVpnStateService(ILogger<IOpenVpnStateService> logger, 
    IOpenVpnMicroserviceClientFactory openVpnMicroserviceClientFactory) : IOpenVpnStateService
{
    private const int RawResponseLogMaxLength = 1024;

    public async Task<OpenVpnState> GetStateAsync(VpnServer openVpnServer, CancellationToken cancellationToken)
    {
        var client = openVpnMicroserviceClientFactory.Create(openVpnServer);
        var response = await client.SendCommandWithResponseAsync("state", cancellationToken);

        logger.LogDebug("Received status response:\n{Response}", response);

        var state = ParseState(response);
        state.RawResponse = response;

        if (state.UpSince <= DateTimeOffset.MinValue)
        {
            logger.LogWarning(
                "[STATE PARSER] UpSince not set after parsing OpenVPN 'state' response. " +
                "Connected={Connected}; Success={Success}; RawResponse={RawResponse}",
                state.Connected,
                state.Success,
                FormatRawResponseForLog(response));
        }

        return state;
    }

    public static string FormatRawResponseForLog(string? rawResponse, int maxLength = RawResponseLogMaxLength)
    {
        if (rawResponse is null)
            return "<null>";

        if (rawResponse.Length == 0)
            return "<empty>";

        if (rawResponse.Length <= maxLength)
            return rawResponse;

        return rawResponse[..maxLength] + $"... (truncated, total {rawResponse.Length} chars)";
    }
    
    private OpenVpnState ParseState(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return new OpenVpnState { Success = false };
        }

        var lines = data.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        OpenVpnState state = new();

        try
        {
            foreach (var line in lines)
            {
                if (OpenVpnManagementResponseLines.IsProtocolLine(line))
                    continue;

                var payloadLine = OpenVpnManagementResponseLines.NormalizeStateCsvLine(line);
                var parts = payloadLine.Split(",");
                if (parts.Length < 5)
                {
                    logger.LogDebug("[STATE PARSER] Skipping non-state line: {Line}", line);
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