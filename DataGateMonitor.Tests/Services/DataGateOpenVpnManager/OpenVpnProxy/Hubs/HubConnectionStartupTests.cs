using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

/// <summary>
/// SignalR <see cref="HubConnectionState"/> has exactly four documented values:
/// Disconnected (0), Connected (1), Connecting (2), Reconnecting (3).
/// </summary>
public class HubConnectionStartupTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(350);

    public static IEnumerable<object[]> DocumentedStates =>
        Enum.GetValues<HubConnectionState>().Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(DocumentedStates))]
    public void HubConnectionState_HasOnlyDocumentedSignalRValues(HubConnectionState state)
    {
        Assert.True(Enum.IsDefined(state));
        Assert.Contains(state, new[]
        {
            HubConnectionState.Disconnected,
            HubConnectionState.Connected,
            HubConnectionState.Connecting,
            HubConnectionState.Reconnecting,
        });
    }

    [Fact]
    public async Task Connected_ReturnsImmediately_WithoutCallingStart()
    {
        var startCalls = 0;

        await HubConnectionStartup.StartWhenReadyAsync(
            () => HubConnectionState.Connected,
            _ =>
            {
                startCalls++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(0, startCalls);
    }

    [Fact]
    public async Task Disconnected_CallsStartOnce()
    {
        var state = HubConnectionState.Disconnected;
        var startCalls = 0;

        await HubConnectionStartup.StartWhenReadyAsync(
            () => state,
            _ =>
            {
                startCalls++;
                state = HubConnectionState.Connected;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(1, startCalls);
        Assert.Equal(HubConnectionState.Connected, state);
    }

    [Fact]
    public async Task Disconnected_PropagatesStartAsyncException()
    {
        var expected = new InvalidOperationException("start failed");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            HubConnectionStartup.StartWhenReadyAsync(
                () => HubConnectionState.Disconnected,
                _ => throw expected,
                CancellationToken.None));

        Assert.Same(expected, ex);
    }

    [Fact]
    public async Task Connecting_WaitsUntilConnected_WithoutCallingStart()
    {
        var state = HubConnectionState.Connecting;
        var startCalls = 0;

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            state = HubConnectionState.Connected;
        });

        await HubConnectionStartup.StartWhenReadyAsync(
            () => state,
            _ =>
            {
                startCalls++;
                return Task.CompletedTask;
            },
            CancellationToken.None,
            TimeSpan.FromSeconds(2));

        Assert.Equal(0, startCalls);
        Assert.Equal(HubConnectionState.Connected, state);
    }

    [Fact]
    public async Task Connecting_ThrowsTimeout_WhenStillConnecting()
    {
        var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
            HubConnectionStartup.StartWhenReadyAsync(
                () => HubConnectionState.Connecting,
                _ => Task.CompletedTask,
                CancellationToken.None,
                ShortTimeout));

        Assert.Contains(nameof(HubConnectionState.Connecting), ex.Message);
    }

    [Fact]
    public async Task Reconnecting_WaitsUntilConnected_WithoutCallingStart()
    {
        var state = HubConnectionState.Reconnecting;
        var startCalls = 0;

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            state = HubConnectionState.Connected;
        });

        await HubConnectionStartup.StartWhenReadyAsync(
            () => state,
            _ =>
            {
                startCalls++;
                return Task.CompletedTask;
            },
            CancellationToken.None,
            TimeSpan.FromSeconds(2));

        Assert.Equal(0, startCalls);
        Assert.Equal(HubConnectionState.Connected, state);
    }

    [Fact]
    public async Task Reconnecting_ThrowsTimeout_WhenStillReconnecting()
    {
        var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
            HubConnectionStartup.StartWhenReadyAsync(
                () => HubConnectionState.Reconnecting,
                _ => Task.CompletedTask,
                CancellationToken.None,
                ShortTimeout));

        Assert.Contains(nameof(HubConnectionState.Reconnecting), ex.Message);
    }

    [Fact]
    public async Task Connecting_CanBeCancelledWhileWaiting()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            HubConnectionStartup.StartWhenReadyAsync(
                () => HubConnectionState.Connecting,
                _ => Task.CompletedTask,
                cts.Token,
                TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Reconnecting_TransitionsToDisconnected_ThenCallsStart()
    {
        var state = HubConnectionState.Reconnecting;
        var startCalls = 0;

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            state = HubConnectionState.Disconnected;
        });

        await HubConnectionStartup.StartWhenReadyAsync(
            () => state,
            _ =>
            {
                startCalls++;
                state = HubConnectionState.Connected;
                return Task.CompletedTask;
            },
            CancellationToken.None,
            TimeSpan.FromSeconds(2));

        Assert.Equal(1, startCalls);
        Assert.Equal(HubConnectionState.Connected, state);
    }

    [Fact]
    public async Task UnknownEnumValue_ThrowsTimeout_WhenItNeverChanges()
    {
        const HubConnectionState unknown = (HubConnectionState)99;

        var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
            HubConnectionStartup.StartWhenReadyAsync(
                () => unknown,
                _ => Task.CompletedTask,
                CancellationToken.None,
                ShortTimeout));

        Assert.Contains("99", ex.Message);
    }
}
