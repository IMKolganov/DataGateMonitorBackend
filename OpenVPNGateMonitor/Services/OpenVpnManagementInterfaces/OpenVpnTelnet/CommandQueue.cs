using System.Collections.Concurrent;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.OpenVpnTelnet;

public class CommandQueue : ICommandQueue
{
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly ConcurrentQueue<PendingCommand> _pendingCommands = new();
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
        if (string.IsNullOrWhiteSpace(message))
            return;

        var trimmed = message.Trim();

        if (trimmed.Contains("END", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("SUCCESS:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("ERROR:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("NOTIFY:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("NOTICE:", StringComparison.OrdinalIgnoreCase))
        {
            if (_pendingCommands.TryDequeue(out var pending))
            {
                if (!pending.TaskSource.Task.IsCompleted)
                    pending.TaskSource.TrySetResult(trimmed);
            }
            else
            {
                Console.WriteLine("[CommandQueue] No pending command found, adding to queue.");
                _messageQueue.Enqueue(trimmed);
                NotifySubscribers(trimmed, cancellationToken);
            }
        }
        else
        {
            Console.WriteLine("[CommandQueue] Message not complete, adding to unprocessed queue.");
            _messageQueue.Enqueue(trimmed);
            NotifySubscribers(trimmed, cancellationToken);
        }
    }

    private void NotifySubscribers(string message, CancellationToken cancellationToken)
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.OnMessageReceived(message, cancellationToken);
        }
    }

    public async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken, int timeoutMs = 15000)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingCommand = new PendingCommand(command, tcs);

        _pendingCommands.Enqueue(pendingCommand);

        await _telnetClient.SendAsync(command, cancellationToken);

        var timeoutTask = Task.Delay(timeoutMs, cancellationToken);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == tcs.Task)
            return await tcs.Task;

        // Если таймаут — задача остаётся в очереди, её нужно удалить (не обязательно, но желательно)
        // Пытаемся вручную удалить первый элемент, если он не завершился
        _ = _pendingCommands.TryDequeue(out _);

        throw new TimeoutException($"[CommandQueue] Command \"{command}\" timed out after {timeoutMs}ms.");
    }

    public (bool result, string? message) TryGetMessage()
    {
        var result = _messageQueue.TryDequeue(out var message);
        return (result, message);
    }

    public async Task DisconnectAsync()
    {
        await _cts.CancelAsync();
        if (!HasSubscribers)
            await _telnetClient.DisconnectAsync();
    }
}