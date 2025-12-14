using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Tests.Helpers;
using System.Collections;

namespace OpenVPNGateMonitor.Tests.Services.Others;

public class SettingsServiceTests
{
    private static (SettingsService svc, Mock<IQueryService<Setting, int>> q, Mock<ICommandService<Setting, int>> cmd)
        CreateService()
    {
        var q = new Mock<IQueryService<Setting, int>>(MockBehavior.Strict);
        var cmd = new Mock<ICommandService<Setting, int>>(MockBehavior.Strict);
        var svc = new SettingsService(q.Object, cmd.Object);
        return (svc, q, cmd);
    }

    // -------- GetValueAsync tests --------

    [Fact]
    public async Task GetValueAsync_ReturnsDefault_When_NotFound()
    {
        var (svc, q, _) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync((Setting?)null);

        var result = await svc.GetValueAsync<string>("NoKey", CancellationToken.None);
        result.Should().BeNull();
        q.VerifyAll();
    }

    [Fact]
    public async Task GetValueAsync_ReturnsDefault_When_ValueType_Null()
    {
        var (svc, q, _) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(new Setting { Id = 1, Key = "A", ValueType = "null" });

        var result = await svc.GetValueAsync<int>("A", CancellationToken.None);
        result.Should().Be(0);
        q.VerifyAll();
    }

    [Fact]
    public async Task GetValueAsync_Returns_Int()
    {
        var (svc, q, _) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(new Setting { Id = 1, Key = "IntKey", ValueType = "int", IntValue = 42 });

        var result = await svc.GetValueAsync<int>("IntKey", CancellationToken.None);
        result.Should().Be(42);
        q.VerifyAll();
    }

    [Fact]
    public async Task GetValueAsync_Returns_Bool()
    {
        var (svc, q, _) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(new Setting { Id = 2, Key = "Flag", ValueType = "bool", BoolValue = true });

        var result = await svc.GetValueAsync<bool>("Flag", CancellationToken.None);
        result.Should().BeTrue();
        q.VerifyAll();
    }

    [Fact]
    public async Task GetValueAsync_Returns_Double()
    {
        var (svc, q, _) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(new Setting { Id = 3, Key = "Pi", ValueType = "double", DoubleValue = 3.14 });

        var result = await svc.GetValueAsync<double>("Pi", CancellationToken.None);
        result.Should().BeApproximately(3.14, 1e-9);
        q.VerifyAll();
    }

    [Fact]
    public async Task GetValueAsync_Returns_DateTimeOffset()
    {
        var moment = DateTimeOffset.UtcNow.AddMinutes(-1);
        var (svc, q, _) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(new Setting { Id = 4, Key = "When", ValueType = "datetime", DateTimeValue = moment });

        var result = await svc.GetValueAsync<DateTimeOffset>("When", CancellationToken.None);
        result.Should().Be(moment);
        q.VerifyAll();
    }

    [Fact]
    public async Task GetValueAsync_Returns_String()
    {
        var (svc, q, _) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(new Setting { Id = 5, Key = "Name", ValueType = "string", StringValue = "Alice" });

        var result = await svc.GetValueAsync<string>("Name", CancellationToken.None);
        result.Should().Be("Alice");
        q.VerifyAll();
    }

    // -------- SetValueAsync tests --------

    [Fact]
    public async Task SetValueAsync_Creates_New_With_Null()
    {
        var (svc, q, cmd) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, false, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync((Setting?)null);

        Setting? saved = null;
        cmd.Setup(c => c.Add(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()))
            .Callback<Setting, bool, CancellationToken>((s, _, _) => saved = s)
            .ReturnsAsync((Setting s, bool _, CancellationToken __) => s);

        await svc.SetValueAsync<object?>("K", null, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Key.Should().Be("K");
        saved.ValueType.Should().Be("null");
        saved.IntValue.Should().BeNull();
        saved.BoolValue.Should().BeNull();
        saved.DoubleValue.Should().BeNull();
        saved.DateTimeValue.Should().BeNull();
        saved.StringValue.Should().BeNull();
        saved.CreateDate.Should().NotBe(default);
        saved.LastUpdate.Should().NotBe(default);

        cmd.Verify(c => c.Add(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()), Times.Once);
        q.VerifyAll();
        cmd.VerifyAll();
    }

    [Fact]
    public async Task SetValueAsync_Creates_New_With_Int()
    {
        var (svc, q, cmd) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, false, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync((Setting?)null);

        Setting? saved = null;
        cmd.Setup(c => c.Add(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()))
            .Callback<Setting, bool, CancellationToken>((s, _, _) => saved = s)
            .ReturnsAsync((Setting s, bool _, CancellationToken __) => s);

        await svc.SetValueAsync("Age", 33, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Key.Should().Be("Age");
        saved.ValueType.Should().Be("int");
        saved.IntValue.Should().Be(33);
        saved.BoolValue.Should().BeNull();
        saved.DoubleValue.Should().BeNull();
        saved.DateTimeValue.Should().BeNull();
        saved.StringValue.Should().BeNull();

        cmd.Verify(c => c.Add(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetValueAsync_Updates_Existing_To_String_And_Resets_Others()
    {
        var existing = new Setting
        {
            Id = 10,
            Key = "UserName",
            CreateDate = DateTimeOffset.UtcNow.AddDays(-1),
            LastUpdate = DateTimeOffset.UtcNow.AddDays(-1),
            ValueType = "int",
            IntValue = 100,
            BoolValue = true,
            DoubleValue = 1.23,
            DateTimeValue = DateTimeOffset.UtcNow.AddHours(-2),
            StringValue = null
        };

        var (svc, q, cmd) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, false, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(existing);

        cmd.Setup(c => c.Update(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await svc.SetValueAsync("UserName", "Bob", CancellationToken.None);

        existing.ValueType.Should().Be("string");
        existing.StringValue.Should().Be("Bob");
        existing.IntValue.Should().BeNull();
        existing.BoolValue.Should().BeNull();
        existing.DoubleValue.Should().BeNull();
        existing.DateTimeValue.Should().BeNull();
        existing.CreateDate.Should().BeOnOrBefore(existing.LastUpdate);

        cmd.Verify(c => c.Update(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetValueAsync_Updates_Existing_To_DateTime()
    {
        var existing = new Setting
        {
            Id = 11,
            Key = "When",
            CreateDate = DateTimeOffset.UtcNow.AddDays(-2),
            LastUpdate = DateTimeOffset.UtcNow.AddDays(-2),
            ValueType = "string",
            StringValue = "old"
        };

        var (svc, q, cmd) = CreateService();
        q.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Setting, bool>>>(), null, false, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Setting, object>>[]>()))
            .ReturnsAsync(existing);

        cmd.Setup(c => c.Update(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var now = DateTimeOffset.UtcNow;
        await svc.SetValueAsync("When", now, CancellationToken.None);

        existing.ValueType.Should().Be("datetime");
        existing.DateTimeValue.Should().Be(now);
        existing.StringValue.Should().BeNull();
        existing.LastUpdate.Should().BeOnOrAfter(existing.CreateDate);

        cmd.Verify(c => c.Update(It.IsAny<Setting>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
