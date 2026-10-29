using DataGateMonitor.SharedModels.DataGateOpenVpnManager.LocalIssuance.Responses;
using System.Net.Http.Headers;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Helpers;

namespace DataGateMonitor.Services.LocalIssuance;

public interface INodeIdentityApiClient
{
    /// <summary>Node's event-signing public key via <c>GET api/node-identity</c> (backend JWT), or null if unreachable.</summary>
    Task<string?> TryGetPublicKeyPemAsync(VpnServer server, CancellationToken ct);
}

public sealed class NodeIdentityApiClient(
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService,
    ILogger<NodeIdentityApiClient> logger) : INodeIdentityApiClient
{
    private const string Endpoint = "api/node-identity";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<string?> TryGetPublicKeyPemAsync(VpnServer server, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            return null;

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
            client.Timeout = Timeout;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", "DataGateOpenVpnManager"));

            using var response = await client.GetAsync(Endpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("Node identity fetch from VpnServer {VpnServerId} returned {Status}",
                    server.Id, (int)response.StatusCode);
                return null;
            }

            var data = await MicroserviceApiResponseHelper.ReadSuccessDataAsync<NodeIdentityResponse>(response, ct);
            return data.PublicKeyPem;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException
                                       or Newtonsoft.Json.JsonException or UriFormatException)
        {
            logger.LogInformation("Node identity fetch from VpnServer {VpnServerId} failed: {Message}", server.Id, ex.Message);
            return null;
        }
    }

}
