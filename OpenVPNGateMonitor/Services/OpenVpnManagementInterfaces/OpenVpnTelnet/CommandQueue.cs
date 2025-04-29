using System.Collections.Concurrent;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.OpenVpnTelnet;

public class CommandQueue : ICommandQueue
{
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly ConcurrentDictionary<Guid, PendingCommand> _pendingCommands = new();
    private readonly TelnetClient _telnetClient;
    private readonly List<IMessageSubscriber> _subscribers = new();
    
    public bool HasSubscribers => _subscribers.Count > 0;

    private readonly CancellationTokenSource _cts = new();

    private record PendingCommand(string CommandText, TaskCompletionSource<string> TaskSource);

    public CommandQueue(TelnetClient telnetClient)
    {
        _telnetClient = telnetClient;
        _telnetClient.OnDataReceived += message => HandleIncomingMessage(message, _cts.Token);
    }

    public void Subscribe(IMessageSubscriber subscriber)
    {
        _subscribers.Add(subscriber);
    }

    public async Task Unsubscribe(IMessageSubscriber subscriber, string ip, int port, ICommandQueueManager queueManager)
    {
        if (!_subscribers.Remove(subscriber))
        {
            throw new Exception("Subscriber doesn't exist");
        }
        if (_subscribers.Count == 0)
        {
            await queueManager.RemoveQueueIfNoSubscribers(ip, port);
        }
    }

    private void HandleIncomingMessage(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        if (message.Contains("END") 
            || message.Contains("SUCCESS:", StringComparison.OrdinalIgnoreCase) 
            || message.Contains("ERROR:", StringComparison.OrdinalIgnoreCase) 
            || message.Contains("NOTIFY:", StringComparison.OrdinalIgnoreCase)
            || message.Contains("NOTICE:", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (cmdId, pending) in _pendingCommands)
            {
                if (message.Contains(pending.CommandText, StringComparison.OrdinalIgnoreCase))
                {
                    if (_pendingCommands.TryRemove(cmdId, out var foundPending))
                    {
                        foundPending.TaskSource.TrySetResult(message.Trim());
                    }
                    return;
                }
            }

            Console.WriteLine("[CommandQueue] No matching pending command found, adding to queue.");
            _messageQueue.Enqueue(message.Trim());
            NotifySubscribers(message, cancellationToken);
        }
        else
        {
            Console.WriteLine("[CommandQueue] Message not complete, adding to unprocessed queue.");
            _messageQueue.Enqueue(message.Trim());
            NotifySubscribers(message, cancellationToken);
        }
    }

    private void NotifySubscribers(string message, CancellationToken cancellationToken)
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.OnMessageReceived(message, cancellationToken);
        }
    }

    public async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken, int timeoutMs = 5000)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        var cmdId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var registration = linkedCts.Token.Register(() => tcs.TrySetCanceled(linkedCts.Token));

        _pendingCommands[cmdId] = new PendingCommand(command, tcs);

        await _telnetClient.SendAsync(command, cancellationToken);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, linkedCts.Token));

        if (completedTask == tcs.Task)
        {
            _pendingCommands.TryRemove(cmdId, out _);
            return await tcs.Task;
        }
        else
        {
            _pendingCommands.TryRemove(cmdId, out _);
            throw new TimeoutException($"[CommandQueue] Command \"{command}\" timed out after {timeoutMs}ms.");
        }
    }

    public (bool result, string? message) TryGetMessage()
    {
        var result = _messageQueue.TryDequeue(out var message);
        return (result, message);
    }

    public async Task DisconnectAsync()
    {
        _cts.Cancel();
        if (!HasSubscribers)
            await _telnetClient.DisconnectAsync();
    }
}