using System.Text.RegularExpressions;
using OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces;

public class OpenVpnVersionService(ILogger<IOpenVpnVersionService> logger, 
    OpenVpnMicroserviceClient microserviceClient) : IOpenVpnVersionService
{
    public async Task<string> GetVersionAsync(int vpnServerId, 
        CancellationToken cancellationToken)
    {
        var response = await microserviceClient.SendCommandWithResponseAsync(
            vpnServerId, "load-stats", cancellationToken);

        logger.LogDebug("Received version response:\n{Response}", response);
        var (openVpnVersion, managementVersion) = ParseVersion(response);
        return openVpnVersion;
    }

    private (string OpenVpnVersion, int ManagementVersion) ParseVersion(string data)
    {
        var openVpnVersion = "Unknown";
        var managementVersion = -1;

        foreach (var line in data.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("OpenVPN Version:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"OpenVPN (\d+\.\d+\.\d+)");
                if (match.Success)
                {
                    openVpnVersion = match.Groups[1].Value;
                }
            }
            else if (line.StartsWith("Management Version:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"Management Version: (\d+)");
                if (match.Success)
                {
                    managementVersion = int.Parse(match.Groups[1].Value);
                }
            }
        }

        return (openVpnVersion, managementVersion);
    }

}