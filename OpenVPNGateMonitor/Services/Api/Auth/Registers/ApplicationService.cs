using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth;

public class ApplicationService(IClientApplicationQueryService clientApplicationQueryService,
    ICommandService<ClientApplication, int> clientApplicationCommandService
    ) : IApplicationService
{
    public async Task<ClientApplication> RegisterApplicationAsync(string name, CancellationToken ct)
    {
        var existClientApplication = await clientApplicationQueryService.GetByName(name, ct);
        
        if (existClientApplication != null)
        {
            throw new Exception("ClientApplication already exists");
        }

        var clientApplication = new ClientApplication()
        {
            Name = name
        };

        await clientApplicationCommandService.Add(clientApplication, true, ct);
        return clientApplication;
    }

    public async Task<ClientApplication?> GetApplicationByClientIdAsync(string clientId, 
        CancellationToken ct)
    {
        return await clientApplicationQueryService.GetByClientId(clientId, ct);
    }
    
    public async Task<ClientApplication?> GetApplicationSystemByClientIdAsync(string clientId, 
        CancellationToken ct)
    {
        return await clientApplicationQueryService.GetBySystemByClientId(clientId, ct);

    }
    
    public async Task<bool> IsSystemApplicationSetAsync(CancellationToken ct)
    {
        var systemApp = await clientApplicationQueryService.IsSystemConfigured(ct);

        return systemApp != null && !string.IsNullOrEmpty(systemApp.ClientSecret);
    }

    public async Task<List<ClientApplication>> GetAllApplicationsAsync(CancellationToken ct)
    {
        return await clientApplicationQueryService.GetAll(ct);
    }
    
    public async Task<ClientApplication> UpdateApplicationAsync(ClientApplication clientApplication, 
        CancellationToken ct)
    {
        await clientApplicationCommandService.Update(clientApplication, true, ct);
    
        return clientApplication;
    }

    public async Task<bool> RevokeApplicationAsync(string clientId, CancellationToken ct)
    {
        var clientApplication = await clientApplicationQueryService.GetByClientId(clientId, ct);

        if (clientApplication == null)
        {
            throw new InvalidOperationException("ClientApplication not found");
        }

        clientApplication.IsRevoked = true;
        
        await clientApplicationCommandService.Update(clientApplication, true, ct);
        return true;
    }
}