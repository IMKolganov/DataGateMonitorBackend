using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.TagTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Tags;
using Xunit;

namespace DataGateMonitor.Tests.Services.Tags;

public class TagServiceTests
{
    private readonly Mock<ICommandService<Tag, int>> _command = new();
    private readonly Mock<ITagQueryService> _query = new();

    private TagService CreateService() => new(_command.Object, _query.Object);

    [Fact]
    public async Task GetAllAsync_DelegatesToQuery()
    {
        var list = new List<Tag> { new() { Id = 1, Name = "A" } };
        _query.Setup(q => q.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var svc = CreateService();
        var result = await svc.GetAllAsync();

        Assert.Same(list, result);
        _query.Verify(q => q.GetAll(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToQuery()
    {
        var tag = new Tag { Id = 7, Name = "X" };
        _query.Setup(q => q.GetById(7, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        var svc = CreateService();
        var result = await svc.GetByIdAsync(7);

        Assert.Same(tag, result);
        _query.Verify(q => q.GetById(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AddsTagWithTrimmedName()
    {
        Tag? captured = null;
        var created = new Tag { Id = 10, Name = "new" };
        _command.Setup(c => c.Add(It.IsAny<Tag>(), true, It.IsAny<CancellationToken>()))
            .Callback<Tag, bool, CancellationToken>((t, _, _) => captured = t)
            .ReturnsAsync(created);

        var svc = CreateService();
        var result = await svc.CreateAsync("  new  ");

        Assert.Same(created, result);
        Assert.NotNull(captured);
        Assert.Equal("new", captured!.Name);
        _command.Verify(c => c.Add(It.IsAny<Tag>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturnsId()
    {
        var existing = new Tag { Id = 5, Name = "old" };
        _query.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _command.Setup(c => c.Update(It.IsAny<Tag>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = CreateService();
        var result = await svc.UpdateAsync(5, "  newName  ");

        Assert.Equal(1, result); // Update returns rows affected
        Assert.Equal("newName", existing.Name);
        _command.Verify(c => c.Update(It.Is<Tag>(t => t.Id == 5 && t.Name == "newName"), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_Throws()
    {
        _query.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((Tag?)null);

        var svc = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(99, "x"));
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToCommand()
    {
        _command.Setup(c => c.DeleteById(3, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = CreateService();
        await svc.DeleteAsync(3);

        _command.Verify(c => c.DeleteById(3, It.IsAny<CancellationToken>()), Times.Once);
    }
}
