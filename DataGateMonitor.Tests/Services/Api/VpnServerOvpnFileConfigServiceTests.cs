using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api;

namespace DataGateMonitor.Tests.Services.Api;

public class VpnServerOvpnFileConfigServiceTests
{
    private static (VpnServerOvpnFileConfigService svc,
        Mock<ILogger<VpnServerOvpnFileConfigService>> log,
        Mock<IVpnServerOvpnFileConfigQueryService> q,
        Mock<ICommandService<VpnServerOvpnFileConfig, int>> cmd)
        CreateService()
    {
        var logger = new Mock<ILogger<VpnServerOvpnFileConfigService>>();
        var q = new Mock<IVpnServerOvpnFileConfigQueryService>(MockBehavior.Strict);
        var cmd = new Mock<ICommandService<VpnServerOvpnFileConfig, int>>(MockBehavior.Strict);
        var svc = new VpnServerOvpnFileConfigService(logger.Object, q.Object, cmd.Object);
        return (svc, logger, q, cmd);
    }

    [Fact]
    public async Task GetByServerId_Returns_Entity_When_Found()
    {
        var (svc, _, q, _) = CreateService();
        var entity = new VpnServerOvpnFileConfig { Id = 1, VpnServerId = 77, VpnServerIp = "1.2.3.4", VpnServerPort = 1194 };
        q.Setup(x => x.GetByVpnServerIdId(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetVpnServerOvpnFileConfigByServerId(77, CancellationToken.None);

        Assert.Same(entity, result);
        q.VerifyAll();
    }

    [Fact]
    public async Task GetByServerId_Throws_When_NotFound()
    {
        var (svc, _, q, _) = CreateService();
        q.Setup(x => x.GetByVpnServerIdId(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServerOvpnFileConfig?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.GetVpnServerOvpnFileConfigByServerId(42, CancellationToken.None));

        Assert.Contains("OvpnFileConfig not found", ex.Message);
        q.VerifyAll();
    }

    [Fact]
    public async Task AddOrUpdate_Updates_Existing_Config()
    {
        var (svc, _, q, cmd) = CreateService();
        var nowBefore = DateTimeOffset.UtcNow;
        var existing = new VpnServerOvpnFileConfig
        {
            Id = 5,
            VpnServerId = 100,
            VpnServerIp = "10.0.0.1",
            VpnServerPort = 1111,
            ConfigTemplate = "old",
            CreateDate = nowBefore.AddDays(-1),
            LastUpdate = nowBefore.AddDays(-1)
        };

        // First call returns existing entity, second call (after update) returns same updated reference
        q.SetupSequence(x => x.GetByVpnServerIdId(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(existing);

        cmd.Setup(c => c.Update(existing, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable();

        var incoming = new VpnServerOvpnFileConfig
        {
            VpnServerId = 100,
            VpnServerIp = "20.0.0.2",
            VpnServerPort = 2222,
            ConfigTemplate = "new"
        };

        var result = await svc.AddOrUpdateVpnServerOvpnFileConfigByServerId(incoming, CancellationToken.None);

        Assert.Same(existing, result);
        Assert.Equal("20.0.0.2", existing.VpnServerIp);
        Assert.Equal(2222, existing.VpnServerPort);
        Assert.Equal("new", existing.ConfigTemplate);
        Assert.True(existing.LastUpdate >= nowBefore);
        cmd.VerifyAll();
        q.VerifyAll();
    }

    [Fact]
    public async Task AddOrUpdate_Creates_New_When_Not_Exists()
    {
        var (svc, _, q, cmd) = CreateService();
        var serverId = 200;

        // Will capture the entity passed to AddAsync
        VpnServerOvpnFileConfig? added = null;

        // First lookup returns null, second re-fetch returns the same object that was added
        q.SetupSequence(x => x.GetByVpnServerIdId(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServerOvpnFileConfig?)null)
            .ReturnsAsync(() => added!);
        cmd.Setup(c => c.Add(It.IsAny<VpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()))
            .Callback<VpnServerOvpnFileConfig, bool, CancellationToken>((e, _, _) => added = e)
            .ReturnsAsync((VpnServerOvpnFileConfig e, bool _, CancellationToken __) => e);

        var incoming = new VpnServerOvpnFileConfig
        {
            VpnServerId = serverId,
            VpnServerIp = "30.0.0.3",
            VpnServerPort = 3333,
            ConfigTemplate = "template"
        };

        var before = DateTimeOffset.UtcNow;
        var result = await svc.AddOrUpdateVpnServerOvpnFileConfigByServerId(incoming, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.NotNull(added);
        Assert.Equal(serverId, added!.VpnServerId);
        Assert.Equal("30.0.0.3", added.VpnServerIp);
        Assert.Equal(3333, added.VpnServerPort);
        Assert.Equal("template", added.ConfigTemplate);
        Assert.InRange(added.CreateDate, before, after);
        Assert.InRange(added.LastUpdate, before, after);

        Assert.Same(added, result);

        cmd.Verify(c => c.Add(It.IsAny<VpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Once);
        q.VerifyAll();
    }

    [Fact]
    public async Task AddOrUpdate_Throws_When_ReFetch_Returns_Null()
    {
        var (svc, _, q, cmd) = CreateService();
        var serverId = 300;

        q.SetupSequence(x => x.GetByVpnServerIdId(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServerOvpnFileConfig?)null)
            .ReturnsAsync((VpnServerOvpnFileConfig?)null);

        cmd.Setup(c => c.Add(It.IsAny<VpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServerOvpnFileConfig e, bool _, CancellationToken __) => e);

        var incoming = new VpnServerOvpnFileConfig
        {
            VpnServerId = serverId,
            VpnServerIp = "40.0.0.4",
            VpnServerPort = 4444,
            ConfigTemplate = "tpl"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AddOrUpdateVpnServerOvpnFileConfigByServerId(incoming, CancellationToken.None));

        Assert.Contains(serverId.ToString(), ex.Message);
        q.VerifyAll();
        cmd.Verify(c => c.Add(It.IsAny<VpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
