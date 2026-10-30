using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.Models;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.Api;

/// <summary>Imports staged credentials only after an administrator has registered the node.</summary>
public sealed class VpnServerPiHoleProvisioningService(
    IQueryService<VpnServer, int> serverQuery,
    IQueryService<VpnServerDiscovery, int> discoveryQuery,
    IQueryService<VpnServerPiHoleConfig, int> configQuery,
    ICommandService<VpnServerDiscovery, int> discoveryCommand,
    IVpnServerPiHoleConfigService configService,
    ILogger<VpnServerPiHoleProvisioningService> logger) : IVpnServerPiHoleProvisioningService
{
    public async Task<bool> ProvisionAsync(int vpnServerId, CancellationToken ct)
    {
        var server = await serverQuery.FindById(vpnServerId, ct: ct)
            ?? throw new InvalidOperationException("VPN server not found.");
        if (!server.IsPiHoleEnabled || server.IsDeleted)
            return false;

        var existing = await configQuery.Query()
            .FirstOrDefaultAsync(c => c.VpnServerId == vpnServerId, ct);
        var candidates = await discoveryQuery.Query(asNoTracking: false)
            .Where(d => d.HasPiHole && d.PiHoleConfigJson != null && d.ServerType == server.ServerType &&
                        (d.ResolvedVpnServerId == vpnServerId ||
                         (d.Status == VpnServerDiscoveryStatus.Pending && d.ResolvedVpnServerId == null)))
            .OrderByDescending(d => d.LastSeenUtc)
            .ToListAsync(ct);
        var discovery = candidates.FirstOrDefault(d =>
            d.Status != VpnServerDiscoveryStatus.Rejected &&
            VpnServerApiUrlHelper.ApiUrlsEquivalent(d.ApiUrl, server.ApiUrl));

        if (discovery is not null)
        {
            var announced = ProjectJson.Deserialize<AnnouncePiHoleConfig>(discovery.PiHoleConfigJson!);
            if (announced is not null &&
                (existing is null ||
                 (string.IsNullOrWhiteSpace(existing.AppPassword) &&
                  existing.BaseUrl.TrimEnd('/') == announced.BaseUrl.TrimEnd('/'))))
            {
                await configService.UpsertAsync(new UpsertVpnServerPiHoleConfigRequest
                {
                    VpnServerId = vpnServerId,
                    BaseUrl = announced.BaseUrl,
                    AppPassword = announced.AppPassword,
                    PollIntervalSeconds = announced.PollIntervalSeconds,
                    BatchSize = announced.BatchSize,
                    LookbackSeconds = announced.LookbackSeconds,
                    ClientSubnetPrefix = announced.ClientSubnetPrefix
                }, ct);
                logger.LogInformation("Imported announced Pi-hole settings for VpnServerId={VpnServerId}.", vpnServerId);
            }
            else if (existing is null || string.IsNullOrWhiteSpace(existing.AppPassword))
            {
                // Never pair node credentials with a different manually configured Pi-hole URL.
                return false;
            }

            discovery.PiHoleConfigJson = null;
            await discoveryCommand.SaveChanges(ct);
        }
        else if (existing is null || string.IsNullOrWhiteSpace(existing.AppPassword))
        {
            return false;
        }

        // A retry uses the saved dashboard configuration even after staged credentials were cleared.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                await configService.ApplyRuntimeToMicroserviceAsync(vpnServerId, timeout.Token);
                return true;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested && attempt < 3 &&
                                       ex is HttpRequestException or OperationCanceledException or InvalidOperationException)
            {
                logger.LogWarning("Pi-hole runtime apply attempt {Attempt} failed for VpnServerId={VpnServerId}; retrying.",
                    attempt, vpnServerId);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
            }
        }
    }
}
