using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.OpenVpnTelnet;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public class OpenVpnServerProcessor(
    ILogger<OpenVpnServerProcessor> logger,
    IServiceProvider serviceProvider,
    ICommandQueueManager commandQueueManager)
{
    public async Task ProcessServerAsync(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        logger.LogInformation($"OpenVpnServerProcessor: " +
                              $"VpnServerId: {openVpnServer.Id}. Vpn Server Name: {openVpnServer.ServerName}. " +
                               $"Processing OpenVPN server: " +
                               $"{openVpnServer.ManagementIp}:{openVpnServer.ManagementPort}");
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var openVpnServerService = scope.ServiceProvider.GetRequiredService<IOpenVpnServerService>();
        try
        {
            //todo: load timeout from config
            var commandQueue = await commandQueueManager.GetOrCreateQueueAsync(
                openVpnServer.ManagementIp, openVpnServer.ManagementPort, cancellationToken, 5);

            logger.LogInformation($"OpenVpnServerProcessor: " +
                                  $"VpnServerId: {openVpnServer.Id}. " +
                                  $"Vpn Server Name: {openVpnServer.ServerName}. " +
                                  $"Saving OpenVPN server status for " +
                                  $"{openVpnServer.ManagementIp}:{openVpnServer.ManagementPort}");
            await openVpnServerService.SaveOpenVpnServerStatusLogAsync(
                openVpnServer.Id,
                commandQueue, 
                cancellationToken);

            logger.LogInformation($"OpenVpnServerProcessor: " +
                                  $"VpnServerId: {openVpnServer.Id}. " +
                                  $"Vpn Server Name: {openVpnServer.ServerName}. " +
                                  $"Saving connected clients for " +
                                  $"{openVpnServer.ManagementIp}:{openVpnServer.ManagementPort}");
            await openVpnServerService.SaveConnectedClientsAsync(
                openVpnServer.Id, 
                commandQueue, 
                cancellationToken);
            
            openVpnServer.IsOnline = true;
            unitOfWork.MarkPropertyModified(openVpnServer, x => x.IsOnline);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"OpenVpnServerProcessor: " +
                                  $"VpnServerId: {openVpnServer.Id}. " +
                                  $"Vpn Server Name: {openVpnServer.ServerName}. " +
                                  $"Finished processing OpenVPN server: " +
                                  $"{openVpnServer.ManagementIp}:{openVpnServer.ManagementPort}");
        }
        catch (Exception ex)
        {
            openVpnServer.IsOnline = false;
            unitOfWork.MarkPropertyModified(openVpnServer, x => x.IsOnline);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogError(ex, $"OpenVpnServerProcessor: " +
                                $"VpnServerId: {openVpnServer.Id}. " +
                                $"Vpn Server Name: {openVpnServer.ServerName}. " +
                                $"OpenVpnServerProcessor: Error processing OpenVPN server " +
                                $"{openVpnServer.ManagementIp}:{openVpnServer.ManagementPort}");
            throw;
        }
    }
}