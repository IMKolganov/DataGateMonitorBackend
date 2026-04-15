using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.Models;
using DataGateMonitor.Services.BackgroundServices.Interfaces;

namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Minimal Xray node polling: HTTP GET to <see cref="VpnServer.ApiUrl"/> must return 2xx (health endpoint).
/// Full client/session sync comes in a later phase.
/// </summary>
public sealed class XrayServerProcessor(
    ILogger<XrayServerProcessor> logger,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider) : IVpnServerWorkProcessor
{
    public const string HttpClientName = "XrayNodeHealth";

    public async Task ProcessServerAsync(VpnServer server, CancellationToken ct)
    {
        logger.LogInformation(
            "XrayServerProcessor: VpnServerId: {Id}. Name: {Name}. Health Url: {Url}",
            server.Id, server.ServerName, server.ApiUrl);

        using var scope = serviceProvider.CreateScope();
        var serverCmd = scope.ServiceProvider.GetRequiredService<ICommandService<VpnServer, int>>();

        try
        {
            if (string.IsNullOrWhiteSpace(server.ApiUrl))
                throw new InvalidOperationException("ApiUrl is required for Xray server health check.");

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, server.ApiUrl.TrimEnd('/'));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Xray node health check returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            var now = DateTimeOffset.UtcNow;
            await serverCmd.UpdateWhere(
                s => s.Id == server.Id,
                u => u.SetProperty(x => x.IsOnline, true)
                    .SetProperty(x => x.LastUpdate, now),
                ct);

            logger.LogInformation(
                "XrayServerProcessor: VpnServerId: {Id}. Name: {Name}. Health check OK.",
                server.Id, server.ServerName);
        }
        catch (Exception ex)
        {
            var now = DateTimeOffset.UtcNow;
            await serverCmd.UpdateWhere(
                s => s.Id == server.Id,
                u => u.SetProperty(x => x.IsOnline, false)
                    .SetProperty(x => x.LastUpdate, now),
                ct);

            logger.LogError(ex,
                "XrayServerProcessor error. VpnServerId: {Id}. Name: {Name}. Url: {Url}",
                server.Id, server.ServerName, server.ApiUrl);

            throw;
        }
    }
}
