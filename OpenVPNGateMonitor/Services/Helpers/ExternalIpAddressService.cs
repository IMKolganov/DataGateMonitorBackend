using OpenVPNGateMonitor.Services.Helpers.Interfaces;

namespace OpenVPNGateMonitor.Services.Helpers;

public class ExternalIpAddressService(ILogger<ExternalIpAddressService> logger, 
    IConfiguration configuration) : IExternalIpAddressService
{
    private readonly List<string>? _externalIpServices = 
        configuration.GetSection("ExternalIpServices").Get<List<string>>();

    public async Task<string> GetRemoteIpAddress(CancellationToken cancellationToken)
    {
        using HttpClient client = new();

        if (_externalIpServices is not { Count: > 0 })
        {
            logger.LogError("No external IP services configured.");
            return "127.0.0.1";
        }

        foreach (string service in _externalIpServices)
        {
            try
            {
                string ip = await client.GetStringAsync(service, cancellationToken);
                logger.LogInformation("Retrieved external IP: {Ip} from {Service}", ip.Trim(), service);
                return ip.Trim();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get IP from {Service}", service);
            }
        }

        logger.LogError("Unable to retrieve external IP from any configured service.");
        return "127.0.0.1";
    }
}