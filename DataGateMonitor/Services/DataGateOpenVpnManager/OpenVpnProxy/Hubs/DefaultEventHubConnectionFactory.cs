using Microsoft.AspNetCore.SignalR.Client;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal sealed class DefaultEventHubConnectionFactory : IEventHubConnectionFactory
{
    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(fullUrl, options => { options.AccessTokenProvider = accessTokenProvider; })
            .AddNewtonsoftJsonProtocol(options => options.PayloadSerializerSettings = ProjectJson.WebSettings)
            .WithAutomaticReconnect([
                TimeSpan.Zero, TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)
            ])
            .Build();

        connection.ServerTimeout = TimeSpan.FromSeconds(30);
        connection.KeepAliveInterval = TimeSpan.FromSeconds(15);

        return new HubConnectionProxy(connection);
    }
}
