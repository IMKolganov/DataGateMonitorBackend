using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces;

public class OpenVpnSummaryStatService(
    ILogger<IOpenVpnSummaryStatService> logger,
    IOpenVpnMicroserviceClientFactory openVpnMicroserviceClientFactory)
    : IOpenVpnSummaryStatService
{
    public async Task<OpenVpnSummaryStats> GetSummaryStatsAsync(VpnServer openVpnServer, 
        CancellationToken cancellationToken)
    {
        var client = openVpnMicroserviceClientFactory.Create(openVpnServer);
        var response = await client.SendCommandWithResponseAsync("load-stats", cancellationToken);

        logger.LogDebug("Received summary stats response:\n{Response}", response);
        return ParseSummaryStats(response);
    }
    
    private OpenVpnSummaryStats ParseSummaryStats(string data)
    {
        OpenVpnSummaryStats stats = new();
        var lines = data.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Contains("nclients="))
            {
                stats.ClientsCount = int.Parse(line.Split("nclients=")[1].Split(',')[0]);
            }
            if (line.Contains("bytesin="))
            {
                stats.BytesIn = long.Parse(line.Split("bytesin=")[1].Split(',')[0]);
            }
            if (line.Contains("bytesout="))
            {
                stats.BytesOut = long.Parse(line.Split("bytesout=")[1].Split(',')[0]);
            }
        }
        return stats;
    }


}