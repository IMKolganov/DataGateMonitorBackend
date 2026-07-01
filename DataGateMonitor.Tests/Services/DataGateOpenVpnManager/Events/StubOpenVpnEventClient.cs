using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.Events;

internal sealed class StubOpenVpnEventClient(
    VpnServer server,
    ILogger<OpenVpnEventClient> logger,
    IHubContext<OpenVpnEventHub> eventHub,
    IMicroserviceTokenService tokenService,
    IServiceScopeFactory scopeFactory) : OpenVpnEventClient(server, logger, eventHub, tokenService, scopeFactory)
{
    public int StartListeningCallCount { get; private set; }

    public override Task StartListeningAsync(CancellationToken cancellationToken)
    {
        StartListeningCallCount++;
        return Task.CompletedTask;
    }
}
