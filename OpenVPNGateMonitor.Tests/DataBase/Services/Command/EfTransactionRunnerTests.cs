using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore.Storage;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Command;

public class EfTransactionRunnerTests
{
    private sealed class Mocks
    {
        public Mock<IUnitOfWork> Uow { get; } = new();
        public Mock<IDbContextTransaction> Tx { get; } = new();

        public Mocks()
        {
            // Setup transaction methods to be awaitable
            Tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Tx.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

            Uow
                .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Tx.Object);
        }
    }

    [Fact]
    public async Task RunAsync_Begins_Transaction_Executes_Action_And_Commits()
    {
        var m = new Mocks();
        var sut = new EfTransactionRunner(m.Uow.Object);

        var executed = false;

        await sut.RunAsync(async ct =>
        {
            await Task.Delay(1, ct);
            executed = true;
        }, CancellationToken.None);

        Assert.True(executed);
        m.Uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        m.Tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        m.Tx.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_T_Generic_Returns_Value_And_Commits()
    {
        var m = new Mocks();
        var sut = new EfTransactionRunner(m.Uow.Object);

        var result = await sut.RunAsync<int>(async ct =>
        {
            await Task.Delay(1, ct);
            return 123;
        }, CancellationToken.None);

        Assert.Equal(123, result);
        m.Uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        m.Tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        m.Tx.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_Does_Not_Commit_When_Action_Throws_And_Disposes()
    {
        var m = new Mocks();
        var sut = new EfTransactionRunner(m.Uow.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.RunAsync(_ => throw new InvalidOperationException("boom"), CancellationToken.None));

        m.Uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        m.Tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        m.Tx.Verify(t => t.DisposeAsync(), Times.Once);
    }
}
