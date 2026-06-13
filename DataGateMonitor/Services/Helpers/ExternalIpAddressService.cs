using DataGateMonitor.Services.Helpers.Interfaces;

namespace DataGateMonitor.Services.Helpers;

public class ExternalIpAddressService(
    ILogger<ExternalIpAddressService> logger,
    IConfiguration configuration,
    HttpClient httpClient)
    : IExternalIpAddressService
{
    private readonly List<string>? _externalIpServices = configuration
        .GetSection("ExternalIpServices")
        .Get<List<string>>();

    public async Task<string> GetRemoteIpAddress(CancellationToken cancellationToken)
    {
        if (_externalIpServices is not { Count: > 0 })
        {
            logger.LogError("No external IP services configured.");
            return "127.0.0.1";
        }

        foreach (var service in _externalIpServices)
        {
            try
            {
                var ip = await httpClient.GetStringAsync(service, cancellationToken);
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