using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class OpenVpnEventClient(
    OpenVpnServer server,
    ILogger<OpenVpnEventClient> logger,
    IHubContext<OpenVpnEventHub> eventHub,
    IMicroserviceTokenService tokenService,
    IServiceProvider serviceProvider)
{
    private readonly OpenVpnServer _server = server;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingCommands = new();
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _handlersRegistered = false;
    
    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);
    }

    private async Task<HubConnection> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null && _connection.State == HubConnectionState.Connected)
                return _connection;

            if (_connection == null)
            {
                logger.LogInformation("Creating SignalR connection for server {ServerId}", _server.Id);
                
                var fullUrl = $"{_server.ApiUrl.TrimEnd('/')}/hubs/openvpn-event";

                _connection = new HubConnectionBuilder()
                    .WithUrl(fullUrl, options =>
                    {
                        options.AccessTokenProvider = () =>
                            Task.FromResult<string?>(tokenService.GenerateToken("vpn-cert-issuer", 
                                "cert-create", "backend", "DataGateCertManager"));
                    })
                    .WithAutomaticReconnect()
                    .Build();

                if (!_handlersRegistered)
                {
                    _connection.On<string>("ReceiveEventMessage", async message =>
                    {
                        try
                        {
                            var data = JsonConvert.DeserializeObject<OpenVpnServerEventLog>(message);
                            if (data is null) return;

                            using var scope = serviceProvider.CreateScope();
                            var logService = scope.ServiceProvider.GetRequiredService<IVpnEventLogService>();

                            await logService.SaveEventAsync(_server.Id, "ReceiveEventMessage", data, message, CancellationToken.None);

                            await eventHub.Clients.Group(_server.Id.ToString())
                                .SendAsync("ReceiveEventMessage", message);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to process and store VPN event from server {ServerId}", _server.Id);
                        }
                    });
                    // _connection.On<string>("ReceiveEventMessage", async message =>
                    // {
                    //     await eventHub.Clients.Group(_server.Id.ToString())
                    //         .SendAsync("ReceiveEventMessage", message);
                    // });

                    _handlersRegistered = true;
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                await _connection.StartAsync(cancellationToken);
                logger.LogInformation("Started OpenVpnEventClient SignalR connection for server {ServerId}", _server.Id);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ReconnectAsync(HubConnection connection)
    {
        try
        {
            await connection.StopAsync();
            await connection.StartAsync();
            logger.LogInformation("Reconnected OpenVpnEventClient to SignalR for server {ServerId}", _server.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed OpenVpnEventClient to reconnect " +
                                "to SignalR for server {ServerId}", _server.Id);
            throw;
        }
    }
}
