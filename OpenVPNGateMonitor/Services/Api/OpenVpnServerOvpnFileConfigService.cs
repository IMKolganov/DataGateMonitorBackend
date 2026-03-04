using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.Api;

public class OpenVpnServerOvpnFileConfigService(
    ILogger<OpenVpnServerOvpnFileConfigService> logger, 
    IOpenVpnServerOvpnFileConfigQueryService  openVpnServerOvpnFileConfigQueryService,
    ICommandService<OpenVpnServerOvpnFileConfig, int> openVpnServerOvpnFileConfigCommandService)
    : IOpenVpnServerOvpnFileConfigService
{
    private readonly ILogger<OpenVpnServerOvpnFileConfigService> _logger = logger;


    public async Task<OpenVpnServerOvpnFileConfig> GetOpenVpnServerOvpnFileConfigByServerId(int vpnServerId, 
        CancellationToken ct)
    {
        return await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdId(vpnServerId, ct)
               ?? throw new InvalidOperationException("OvpnFileConfig not found");
    }
    
    public async Task<OpenVpnServerOvpnFileConfig> AddOrUpdateOpenVpnServerOvpnFileConfigByServerId(
        OpenVpnServerOvpnFileConfig openVpnServerOvpnFileConfig, CancellationToken ct)
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