using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public class OpenVpnServerProcessor(
    ILogger<OpenVpnServerProcessor> logger,
    IServiceProvider serviceProvider)
{
    public async Task ProcessServerAsync(OpenVpnServer openVpnServer, CancellationToken ct)
    {
        logger.LogInformation(
            "OpenVpnServerProcessor: VpnServerId: {Id}. Name: {Name}. Processing: {Url}",
            openVpnServer.Id, openVpnServer.ServerName, openVpnServer.ApiUrl);

        using var scope = serviceProvider.CreateScope();
        var openVpnServerService = scope.ServiceProvider.GetRequiredService<IOpenVpnServerService>();
        var serverCmd = scope.ServiceProvider.GetRequiredService<ICommandService<OpenVpnServer, int>>();

        try
        {
            logger.LogInformation("Saving status log for {Url}", openVpnServer.ApiUrl);
            await openVpnServerService.SaveOpenVpnServerStatusLogAsync(openVpnServer, ct);

            logger.LogInformation("Saving connected clients for {Url}", openVpnServer.ApiUrl);
            await openVpnServerService.SaveConnectedClientsAsync(openVpnServer, ct);

            // Set IsOnline = true (server-side update, no entity tracking)
            var now = DateTimeOffset.UtcNow;
            await serverCmd.UpdateWhereAsync(
                s => s.Id == openVpnServer.Id,
                u => u.SetProperty(x => x.IsOnline, true)
                    .SetProperty(x => x.LastUpdate, now),
                ct);

            logger.LogInformation(
                "OpenVpnServerProcessor: VpnServerId: {Id}. Name: {Name}. Finished processing: {Url}",
                openVpnServer.Id, openVpnServer.ServerName, openVpnServer.ApiUrl);
        }
        catch (Exception ex)
        {
            // Mark server offline on failure
            var now = DateTimeOffset.UtcNow;
            await serverCmd.UpdateWhereAsync(
                s => s.Id == openVpnServer.Id,
                u => u.SetProperty(x => x.IsOnline, false)
                    .SetProperty(x => x.LastUpdate, now),
                ct);

            logger.LogError(ex,
                "OpenVpnServerProcessor error. VpnServerId: {Id}. Name: {Name}. Url: {Url}",
                openVpnServer.Id, openVpnServer.ServerName, openVpnServer.ApiUrl);

            throw;
        }
    }
}