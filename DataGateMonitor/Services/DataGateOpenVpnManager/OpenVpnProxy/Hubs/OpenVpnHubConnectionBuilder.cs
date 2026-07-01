using Microsoft.AspNetCore.SignalR.Client;
using DataGateMonitor.Serialization;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal static class OpenVpnHubConnectionBuilder
{
    internal static HubConnection Build(string fullUrl, Func<Task<string?>> accessTokenProvider, bool suppressSignalRInfoLogs = false)
    {
        var builder = new HubConnectionBuilder()
            .WithUrl(fullUrl, options => { options.AccessTokenProvider = accessTokenProvider; })
            .AddNewtonsoftJsonProtocol(options => options.PayloadSerializerSettings = ProjectJson.WebSettings)
            .WithAutomaticReconnect(OpenVpnHubConnectionDefaults.AutomaticReconnectDelays);

        if (suppressSignalRInfoLogs)
        {
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        }

        var connection = builder.Build();
        connection.ServerTimeout = OpenVpnHubConnectionDefaults.ServerTimeout;
        connection.KeepAliveInterval = OpenVpnHubConnectionDefaults.KeepAliveInterval;
        return connection;
    }
}
