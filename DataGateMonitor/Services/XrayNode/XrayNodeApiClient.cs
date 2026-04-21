using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DataGateMonitor.Models.XrayNode;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace DataGateMonitor.Services.XrayNode;

public sealed class XrayNodeApiClient(
    ILogger<XrayNodeApiClient> logger,
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService) : IXrayNodeApiClient
{
    public const string HttpClientName = "XrayNodeApi";

    internal const string ClientsRelativePath = "api/xray/clients";
    internal const string KickRelativePath = "api/xray/clients/kick";
    internal const string DisableRelativePath = "api/xray/users/disable";
    private const string AudienceXRayManager = "DataGateXRayManager";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<XrayNodeClientsResponse?> GetActiveClientsAsync(string baseApiUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseApiUrl))
            throw new ArgumentException("Base API URL is required.", nameof(baseApiUrl));

        var baseUri = baseApiUrl.TrimEnd('/') + "/";
        var requestUri = new Uri(new Uri(baseUri, UriKind.Absolute), ClientsRelativePath);

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", AudienceXRayManager));

        using var response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Xray node clients request failed: {StatusCode} {Reason} for {Uri}",
                (int)response.StatusCode, response.ReasonPhrase, requestUri);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<XrayNodeClientsResponse>(stream, JsonOptions,
            cancellationToken);

        return result ?? new XrayNodeClientsResponse();
    }

    public async Task KickUserAsync(string baseApiUrl, string commonName, CancellationToken cancellationToken)
    {
        await PostJsonExpectSuccessAsync(baseApiUrl, KickRelativePath,
            new { commonName }, cancellationToken, "kick");
    }

    public async Task DisableUserAsync(string baseApiUrl, string commonName, CancellationToken cancellationToken)
    {
        await PostJsonExpectSuccessAsync(baseApiUrl, DisableRelativePath,
            new { commonName }, cancellationToken, "disable user");
    }

    private async Task PostJsonExpectSuccessAsync(string baseApiUrl, string relativePath, object body,
        CancellationToken cancellationToken, string actionLabel)
    {
        if (string.IsNullOrWhiteSpace(baseApiUrl))
            throw new ArgumentException("Base API URL is required.", nameof(baseApiUrl));

        var baseUri = baseApiUrl.TrimEnd('/') + "/";
        var requestUri = new Uri(new Uri(baseUri, UriKind.Absolute), relativePath);
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", AudienceXRayManager));

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Xray node {Action} failed: {Status} {Body}", actionLabel,
                (int)response.StatusCode, raw);
            throw new HttpRequestException(
                $"Xray node {actionLabel} failed ({(int)response.StatusCode}): {raw}");
        }
    }
}
