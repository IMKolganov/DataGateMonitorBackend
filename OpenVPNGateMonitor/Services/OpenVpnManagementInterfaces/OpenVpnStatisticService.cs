using OpenVPNGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces;

public class OpenVpnSummaryStatService : IOpenVpnSummaryStatService
{
    private readonly ILogger<IOpenVpnSummaryStatService> _logger;
    private readonly OpenVpnMicroserviceClient _microserviceClient;
    
    public OpenVpnSummaryStatService(ILogger<IOpenVpnSummaryStatService> logger,
        OpenVpnMicroserviceClient microserviceClient)
    {
        _logger = logger;
        _microserviceClient = microserviceClient;
    }
    
    public async Task<OpenVpnSummaryStats> GetSummaryStatsAsync(int vpnServerId, 
        CancellationToken cancellationToken)
    {
        var response = await _microserviceClient.SendCommandWithResponseAsync(
            vpnServerId, "load-stats", cancellationToken);

        _logger.LogDebug("Received summary stats response:\n{Response}", response);
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