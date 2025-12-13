using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientFactory(IServiceProvider serviceProvider) : IOpenVpnMicroserviceClientFactory
{
    private readonly ConcurrentDictionary<int, IOpenVpnMicroserviceClient> _clientCache = new();

    public IOpenVpnMicroserviceClient Create(OpenVpnServer server)
    {
        var normalizedUrl = NormalizeUrl(server.ApiUrl);

        return _clientCache.AddOrUpdate(
            server.Id,
            _ => CreateNew(server),
            (_, existing) =>
            {
                // compare normalized
                if (!UrlsEqual(existing.CurrentApiUrl, normalizedUrl))
                {
                    DisposeClient(existing);
                    return CreateNew(server);
                }
                return existing;
            });
    }

    public async Task<IOpenVpnMicroserviceClient?> TryCreateByServerIdAsync(
        int serverId,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var openVpnOverviewQuery = scope.ServiceProvider.GetRequiredService<IOpenVpnServerQueryService>();
        var server = await openVpnOverviewQuery.GetByIdAsync(serverId, cancellationToken)
                     ?? throw new Exception($"OpenVPN server not found with id {serverId}");

        return Create(server);
    }

    private IOpenVpnMicroserviceClient CreateNew(OpenVpnServer server)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OpenVpnMicroserviceClient>>();
        var frontendHub = scope.ServiceProvider.GetRequiredService<IHubContext<OpenVpnFrontendHub>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IMicroserviceTokenService>();
        return new OpenVpnMicroserviceClient(server, logger, frontendHub, tokenService);
    }

    private static void DisposeClient(IOpenVpnMicroserviceClient client)
    {
        // try async dispose first; fall back to sync
        (client as IAsyncDisposable)?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        (client as IDisposable)?.Dispose();
    }

    private static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            var left = uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath;
            return left.TrimEnd('/').ToLowerInvariant();
        }
        catch
        {
            return url.Trim().TrimEnd('/').ToLowerInvariant();
        }
    }

    private static bool UrlsEqual(string? a, string? b) => NormalizeUrl(a) == NormalizeUrl(b);
}