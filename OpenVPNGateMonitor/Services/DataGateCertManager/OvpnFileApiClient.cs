using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class OvpnFileApiClient(HttpClient httpClient, ILogger<OvpnFileApiClient> logger) : IOvpnFileApiClient
{
}