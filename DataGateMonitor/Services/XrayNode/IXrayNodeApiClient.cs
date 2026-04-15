using DataGateMonitor.Models.XrayNode;

namespace DataGateMonitor.Services.XrayNode;

/// <summary>
/// Calls the Xray node agent HTTP API (same base URL as the VPN server row <c>ApiUrl</c>).
/// </summary>
public interface IXrayNodeApiClient
{
    /// <summary>
    /// GET <c>{baseUrl}/api/xray/clients</c>. Returns null if the response is empty or not JSON.
    /// </summary>
    Task<XrayNodeClientsResponse?> GetActiveClientsAsync(string baseApiUrl, CancellationToken cancellationToken);
}
