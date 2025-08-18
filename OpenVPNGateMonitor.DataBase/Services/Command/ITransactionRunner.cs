namespace OpenVPNGateMonitor.DataBase.Services.Command;

// Interface
public interface ITransactionRunner
{
    Task RunAsync(Func<CancellationToken, Task> action, CancellationToken ct);
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}
