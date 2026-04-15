using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Interfaces;

namespace DataGateMonitor.Services.Api;

public class VpnServerOvpnFileConfigService(
    ILogger<VpnServerOvpnFileConfigService> logger, 
    IVpnServerOvpnFileConfigQueryService  openVpnServerOvpnFileConfigQueryService,
    ICommandService<VpnServerOvpnFileConfig, int> openVpnServerOvpnFileConfigCommandService)
    : IVpnServerOvpnFileConfigService
{
    private readonly ILogger<VpnServerOvpnFileConfigService> _logger = logger;


    public async Task<VpnServerOvpnFileConfig> GetVpnServerOvpnFileConfigByServerId(int vpnServerId, 
        CancellationToken ct)
    {
        return await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdId(vpnServerId, ct)
               ?? throw new InvalidOperationException("OvpnFileConfig not found");
    }
    
    public async Task<VpnServerOvpnFileConfig> AddOrUpdateVpnServerOvpnFileConfigByServerId(
        VpnServerOvpnFileConfig openVpnServerOvpnFileConfig, CancellationToken ct)
    {

        var existingConfig = await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdId(
            openVpnServerOvpnFileConfig.VpnServerId, ct);

        if (existingConfig != null)
        {
            existingConfig.VpnServerIp = openVpnServerOvpnFileConfig.VpnServerIp;
            existingConfig.VpnServerPort = openVpnServerOvpnFileConfig.VpnServerPort;
            existingConfig.ConfigTemplate = openVpnServerOvpnFileConfig.ConfigTemplate;
            existingConfig.LastUpdate = DateTimeOffset.UtcNow;

            await openVpnServerOvpnFileConfigCommandService.Update(existingConfig, true, ct);
        }
        else
        {
            openVpnServerOvpnFileConfig.CreateDate = DateTimeOffset.UtcNow;
            openVpnServerOvpnFileConfig.LastUpdate = DateTimeOffset.UtcNow;
            
            await openVpnServerOvpnFileConfigCommandService.Add(openVpnServerOvpnFileConfig, true, ct);
        }
        
        return await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdId(
                   openVpnServerOvpnFileConfig.VpnServerId, ct)
               ?? throw new InvalidOperationException($"OpenVPN server OVPN file configuration not found for " +
                                                      $"server ID {openVpnServerOvpnFileConfig.VpnServerId}.");
    }

    
}