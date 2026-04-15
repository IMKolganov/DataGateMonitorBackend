using System.Net.Http.Json;
using System.Text.Json;
using DataGateMonitor.Models.XrayNode;

namespace DataGateMonitor.Services.XrayNode;

public sealed class XrayNodeApiClient(
    ILogger<XrayNodeApiClient> logger,
    IHttpClientFactory httpClientFactory) : IXrayNodeApiClient
{
    public const string HttpClientName = "XrayNodeApi";

    internal const string ClientsRelativePath = "api/xray/clients";

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
        using var response = await client.GetAsync(requestUri, cancellationToken);

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
}
