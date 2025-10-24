using OpenVPNGateMonitor.DataBase.UnitOfWork;

namespace OpenVPNGateMonitor.DataBase.Services.Command;

public class EfTransactionRunner(IUnitOfWork uow) : ITransactionRunner
{
    public async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        await action(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var result = await action(ct);
        await tx.CommitAsync(ct);
        return result;
    }
}