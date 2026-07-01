using Microsoft.AspNetCore.SignalR.Client;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal static class HubConnectionStartup
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan DefaultTransitionTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Starts the hub only when <see cref="HubConnectionState.Disconnected"/>.
    /// Waits while automatic reconnect is in progress (<see cref="HubConnectionState.Connecting"/> /
    /// <see cref="HubConnectionState.Reconnecting"/>) instead of calling <c>StartAsync</c> and failing.
    /// </summary>
    public static Task StartWhenReadyAsync(
        Func<HubConnectionState> getState,
        Func<CancellationToken, Task> startAsync,
        CancellationToken cancellationToken,
        TimeSpan? transitionTimeout = null)
        => StartWhenReadyAsync(getState, startAsync, cancellationToken, transitionTimeout ?? DefaultTransitionTimeout);

    private static async Task StartWhenReadyAsync(
        Func<HubConnectionState> getState,
        Func<CancellationToken, Task> startAsync,
        CancellationToken cancellationToken,
        TimeSpan transitionTimeout)
    {
        var deadline = DateTime.UtcNow + transitionTimeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = getState();

            if (state == HubConnectionState.Connected)
                return;

            if (state is HubConnectionState.Connecting or HubConnectionState.Reconnecting)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException(
                        $"Hub connection remained in {state} for longer than {transitionTimeout}.");
                }

                await Task.Delay(DefaultPollInterval, cancellationToken);
                continue;
            }

            if (state == HubConnectionState.Disconnected)
            {
                await startAsync(cancellationToken);
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Hub connection remained in {state} for longer than {transitionTimeout}.");
            }

            await Task.Delay(DefaultPollInterval, cancellationToken);
        }
    }
}
