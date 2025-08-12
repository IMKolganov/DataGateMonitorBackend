using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.Api;

public class OpenVpnServerOvpnFileConfigService(
    ILogger<OpenVpnServerOvpnFileConfigService> logger, 
    IOpenVpnServerOvpnFileConfigQueryService  openVpnServerOvpnFileConfigQueryService)
    : IOpenVpnServerOvpnFileConfigService
{
    private readonly ILogger<OpenVpnServerOvpnFileConfigService> _logger = logger;


    public async Task<OpenVpnServerOvpnFileConfig> GetOpenVpnServerOvpnFileConfigByServerId(int vpnServerId, 
        CancellationToken ct)
    {
        return await openVpnServerOvpnFileConfigQueryService.GetByServerIdIdAsync(vpnServerId, ct)
               ?? throw new InvalidOperationException("OvpnFileConfig not found");
    }
    
    public async Task<OpenVpnServerOvpnFileConfig> AddOrUpdateOpenVpnServerOvpnFileConfigByServerId(
        OpenVpnServerOvpnFileConfig openVpnServerOvpnFileConfig, CancellationToken ct)
    {
        var openVpnServerOvpnFileConfigRepository = unitOfWork.GetRepository<OpenVpnServerOvpnFileConfig>();

        var existingConfig = await openVpnServerOvpnFileConfigQueryService.GetByServerIdIdAsync(
            openVpnServerOvpnFileConfig.VpnServerId, ct);

        if (existingConfig != null)
        {
            existingConfig.VpnServerIp = openVpnServerOvpnFileConfig.VpnServerIp;
            existingConfig.VpnServerPort = openVpnServerOvpnFileConfig.VpnServerPort;
            existingConfig.ConfigTemplate = openVpnServerOvpnFileConfig.ConfigTemplate;
            existingConfig.LastUpdate = DateTime.UtcNow;

            openVpnServerOvpnFileConfigRepository.Update(existingConfig);
        }
        else
        {
            openVpnServerOvpnFileConfig.CreateDate = DateTime.UtcNow;
            openVpnServerOvpnFileConfig.LastUpdate = DateTime.UtcNow;

            await openVpnServerOvpnFileConfigRepository.AddAsync(openVpnServerOvpnFileConfig, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return await openVpnServerOvpnFileConfigQueryService.GetByServerIdIdAsync(
                   openVpnServerOvpnFileConfig.VpnServerId, ct)
               ?? throw new InvalidOperationException($"OpenVPN server OVPN file configuration not found for " +
                                                      $"server ID {openVpnServerOvpnFileConfig.VpnServerId}.");
    }

    
}