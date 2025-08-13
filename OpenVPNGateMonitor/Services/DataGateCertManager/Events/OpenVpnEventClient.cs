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
                logger.LogInformation("Creating SignalR connection for server {ServerId}", server.Id);
                
                var fullUrl = $"{server.ApiUrl.TrimEnd('/')}/hubs/openvpn-event";

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
                    _connection.On<OpenVpnServerEventLog>("ClientConnected", async data =>
                    {
                        await HandleEvent("ClientConnected", data);
                    });

                    _connection.On<OpenVpnServerEventLog>("ClientDisconnected", async data =>
                    {
                        await HandleEvent("ClientDisconnected", data);
                    });

                    _connection.On<OpenVpnServerEventLog>("ClientAttempted", async data =>
                    {
                        await HandleEvent("ClientAttempted", data);
                    });

                    _connection.On<OpenVpnServerEventLog>("TlsVerified", async data =>
                    {
                        await HandleEvent("TlsVerified", data);
                    });

                    _handlersRegistered = true;
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                await _connection.StartAsync(cancellationToken);
                logger.LogInformation("Started OpenVpnEventClient SignalR connection for server {ServerId}", server.Id);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
    
    private async Task HandleEvent(string eventType, OpenVpnServerEventLog data)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IVpnEventLogService>();

            var rawJson = JsonConvert.SerializeObject(data);

            var log = new OpenVpnServerEventLog
            {
                CommonName = data.CommonName,
                RealAddress = data.RealAddress,
                VirtualAddress = data.VirtualAddress,
                ConnectedSince = data.ConnectedSince,
                Message = data.Message,
                RawJson = rawJson
            };

            await logService.SaveEventAsync(server.Id, eventType, log, rawJson, CancellationToken.None);

            await eventHub.Clients.Group(server.Id.ToString())
                .SendAsync(eventType, data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle SignalR event {EventType} from server {ServerId}", eventType, server.Id);
        }
    }


    private async Task ReconnectAsync(HubConnection connection)
    {
        try
        {
            await connection.StopAsync();
            await connection.StartAsync();
            logger.LogInformation("Reconnected OpenVpnEventClient to SignalR for server {ServerId}", server.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed OpenVpnEventClient to reconnect " +
                                "to SignalR for server {ServerId}", server.Id);
            throw;
        }
    }
}
