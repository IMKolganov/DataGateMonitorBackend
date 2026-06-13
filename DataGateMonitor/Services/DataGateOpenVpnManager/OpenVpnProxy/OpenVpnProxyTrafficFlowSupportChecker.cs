using System.Collections.Concurrent;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public sealed class OpenVpnProxyTrafficFlowSupportChecker(
    IServiceScopeFactory scopeFactory,
    ILogger<OpenVpnProxyTrafficFlowSupportChecker> logger) : IOpenVpnProxyTrafficFlowSupportChecker
{
    public const string MinMicroserviceVersion = "1.2.5.54";

    private static readonly TimeSpan SupportedCacheTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan UnsupportedCacheTtl = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<int, CacheEntry> _cache = new();

    public async Task<bool> ShouldListenAsync(VpnServer server, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
        {
            logger.LogDebug(
                "Proxy traffic flow skipped for server {ServerId}: ApiUrl is not configured",
                server.Id);
            return false;
        }

        var apiUrl = server.ApiUrl.Trim();
        if (_cache.TryGetValue(server.Id, out var cached)
            && string.Equals(cached.ApiUrl, apiUrl, StringComparison.OrdinalIgnoreCase)
            && cached.ExpiresAtUtc > DateTime.UtcNow)
        {
            return cached.Supported;
        }

        var supported = await EvaluateAsync(server, apiUrl, cancellationToken);
        _cache[server.Id] = new CacheEntry(
            apiUrl,
            supported,
            DateTime.UtcNow.Add(supported ? SupportedCacheTtl : UnsupportedCacheTtl));
        return supported;
    }

    private async Task<bool> EvaluateAsync(VpnServer server, string apiUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var infoService = scope.ServiceProvider.GetRequiredService<IMicroserviceInfoService>();
            var info = await infoService.GetInfoByUrlAsync(apiUrl, VpnServerType.OpenVpn, cancellationToken);
            var version = info?.OpenVpn?.Version?.Trim();

            if (string.IsNullOrWhiteSpace(version))
            {
                logger.LogDebug(
                    "Proxy traffic flow skipped for server {ServerId}: microservice version is unavailable",
                    server.Id);
                return false;
            }

            if (!DotVersionComparer.IsAtLeast(version, MinMicroserviceVersion))
            {
                logger.LogDebug(
                    "Proxy traffic flow skipped for server {ServerId}: microservice {Version} < {MinVersion}",
                    server.Id,
                    version,
                    MinMicroserviceVersion);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Proxy traffic flow skipped for server {ServerId}: failed to read microservice info",
                server.Id);
            return false;
        }
    }

    private sealed record CacheEntry(string ApiUrl, bool Supported, DateTime ExpiresAtUtc);
}
