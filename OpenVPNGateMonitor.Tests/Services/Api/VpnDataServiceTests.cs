using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Events;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Api;

public class VpnDataServiceTests
{
    private static (VpnDataService svc,
        Mock<ILogger<IVpnDataService>> log,
        Mock<IExternalIpAddressService> ip,
        Mock<IQuotaPlanQueryService> quotaPlanQ,
        Mock<IOpenVpnServerQueryService> serverQ,
        Mock<IOpenVpnServerOvpnFileConfigQueryService> cfgQ,
        Mock<ITransactionRunner> trx,
        Mock<ICommandService<OpenVpnServer, int>> serverCmd,
        Mock<ICommandService<OpenVpnServerOvpnFileConfig, int>> cfgCmd,
        Mock<ICommandService<QuotaPlanAllowedServer, int>> quotaPlanCmd,
        Mock<ICommandService<OpenVpnServerTag, int>> tagCmd,
        Mock<IServerOpenVpnNotificationService> notification,
        Mock<IOpenVpnMicroserviceClientFactory> microserviceFactory,
        Mock<IOpenVpnEventClientFactory> eventFactory)
        CreateService()
    {
        var log = new Mock<ILogger<IVpnDataService>>();
        var ip = new Mock<IExternalIpAddressService>(MockBehavior.Strict);
        var serverQ = new Mock<IOpenVpnServerQueryService>(MockBehavior.Strict);
        var cfgQ = new Mock<IOpenVpnServerOvpnFileConfigQueryService>(MockBehavior.Strict);
        var trx = new Mock<ITransactionRunner>(MockBehavior.Strict);
        var serverCmd = new Mock<ICommandService<OpenVpnServer, int>>(MockBehavior.Strict);
        var cfgCmd = new Mock<ICommandService<OpenVpnServerOvpnFileConfig, int>>(MockBehavior.Strict);
        var quotaPlanCmd = new Mock<ICommandService<QuotaPlanAllowedServer, int>>(MockBehavior.Strict);
        var tagCmd = new Mock<ICommandService<OpenVpnServerTag, int>>(MockBehavior.Strict);
        var notification = new Mock<IServerOpenVpnNotificationService>(MockBehavior.Loose);
        var microserviceFactory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Loose);
        var eventFactory = new Mock<IOpenVpnEventClientFactory>(MockBehavior.Loose);
        var quotaPlanQ = new Mock<IQuotaPlanQueryService>(MockBehavior.Strict);
        quotaPlanQ.Setup(q => q.GetDefault(It.IsAny<CancellationToken>())).ReturnsAsync((QuotaPlan?)null);

        trx.Setup(t => t.RunAsync(It.IsAny<Func<CancellationToken, Task<OpenVpnServer>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<OpenVpnServer>>, CancellationToken>(async (f, ct) => await f(ct));
        trx.Setup(t => t.RunAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (f, ct) => await f(ct));

        var svc = new VpnDataService(
            log.Object,
            ip.Object,
            quotaPlanQ.Object,
            serverQ.Object,
            cfgQ.Object,
            trx.Object,
            serverCmd.Object,
            cfgCmd.Object,
            quotaPlanCmd.Object,
            tagCmd.Object,
            notification.Object,
            microserviceFactory.Object,
            eventFactory.Object);

        return (svc, log, ip, quotaPlanQ, serverQ, cfgQ, trx, serverCmd, cfgCmd, quotaPlanCmd, tagCmd, notification, microserviceFactory, eventFactory);
    }

    [Fact]
    public async Task AddOpenVpnServer_Adds_Config_When_None_Exists_And_Not_Default()
    {
        var (svc, _, ip, _, serverQ, cfgQ, _, serverCmd, cfgCmd, quotaPlanCmd, tagCmd, _, _, _) = CreateService();
        var server = new OpenVpnServer { Id = 0, IsDefault = false, ServerName = "Srv" };

        var before = DateTimeOffset.UtcNow;

        cfgQ.Setup(q => q.AnyByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("203.0.113.10");
        serverQ.Setup(q => q.AnyByServerName("Srv", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        serverCmd.Setup(c => c.Add(It.IsAny<OpenVpnServer>(), true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServer, bool, CancellationToken>((s, _, _) => s.Id = 101)
            .ReturnsAsync((OpenVpnServer s, bool _, CancellationToken _) => s);

        OpenVpnServerOvpnFileConfig? addedCfg = null;
        cfgCmd.Setup(c => c.Add(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServerOvpnFileConfig, bool, CancellationToken>((e, _, _) => addedCfg = e)
            .ReturnsAsync((OpenVpnServerOvpnFileConfig e, bool _, CancellationToken _) => e);

        serverQ.Setup(q => q.GetById(101, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        quotaPlanCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<QuotaPlanAllowedServer, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
        tagCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<OpenVpnServerTag, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

        var result = await svc.AddOpenVpnServer(server, new List<int>(), new List<int>(), CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(101, result.Id);
        Assert.InRange(server.CreateDate, before, after);
        Assert.InRange(server.LastUpdate, before, after);

        Assert.NotNull(addedCfg);
        Assert.Equal(101, addedCfg!.VpnServerId);
        Assert.Equal("203.0.113.10", addedCfg.VpnServerIp);

        cfgCmd.Verify(c => c.Add(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Once);
        serverCmd.Verify(c => c.Add(It.IsAny<OpenVpnServer>(), true, It.IsAny<CancellationToken>()), Times.Once);
        serverQ.Verify(q => q.GetById(101, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOpenVpnServer_Links_DefaultQuotaPlan_When_List_Empty_And_Default_Exists()
    {
        var (svc, _, ip, quotaPlanQ, serverQ, cfgQ, _, serverCmd, cfgCmd, quotaPlanCmd, tagCmd, _, _, _) = CreateService();
        quotaPlanQ.Setup(q => q.GetDefault(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuotaPlan { Id = 42, Name = "Default" });

        var server = new OpenVpnServer { Id = 0, IsDefault = false, ServerName = "Srv" };

        cfgQ.Setup(q => q.AnyByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("203.0.113.10");
        serverQ.Setup(q => q.AnyByServerName("Srv", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        serverCmd.Setup(c => c.Add(It.IsAny<OpenVpnServer>(), true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServer, bool, CancellationToken>((s, _, _) => s.Id = 101)
            .ReturnsAsync((OpenVpnServer s, bool _, CancellationToken _) => s);

        cfgCmd.Setup(c => c.Add(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenVpnServerOvpnFileConfig e, bool _, CancellationToken _) => e);

        serverQ.Setup(q => q.GetById(101, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        quotaPlanCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<QuotaPlanAllowedServer, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
        quotaPlanCmd.Setup(c => c.AddRange(It.IsAny<IEnumerable<QuotaPlanAllowedServer>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        tagCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<OpenVpnServerTag, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

        await svc.AddOpenVpnServer(server, new List<int>(), new List<int>(), CancellationToken.None);

        quotaPlanCmd.Verify(c => c.AddRange(
            It.Is<IEnumerable<QuotaPlanAllowedServer>>(xs =>
                xs.Single().QuotaPlanId == 42 && xs.Single().VpnServerId == 101),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOpenVpnServer_Unsets_Previous_Default_When_IsDefault_True()
    {
        var (svc, _, ip, _, serverQ, cfgQ, _, serverCmd, cfgCmd, quotaPlanCmd, tagCmd, _, _, _) = CreateService();
        var server = new OpenVpnServer { Id = 0, IsDefault = true, ServerName = "DefaultSrv" };

        cfgQ.Setup(q => q.AnyByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("ignored");
        serverQ.Setup(q => q.AnyByServerName("DefaultSrv", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        serverCmd.Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<OpenVpnServer>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        serverCmd.Setup(c => c.Add(It.IsAny<OpenVpnServer>(), true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServer, bool, CancellationToken>((s, _, _) => s.Id = 7)
            .ReturnsAsync((OpenVpnServer s, bool _, CancellationToken _) => s);

        serverQ.Setup(q => q.GetById(7, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        quotaPlanCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<QuotaPlanAllowedServer, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
        tagCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<OpenVpnServerTag, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

        var result = await svc.AddOpenVpnServer(server, new List<int>(), new List<int>(), CancellationToken.None);

        Assert.Equal(7, result.Id);
        serverCmd.Verify(c => c.UpdateWhere(
                It.IsAny<Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<OpenVpnServer>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        cfgCmd.Verify(c => c.Add(It.IsAny<OpenVpnServerOvpnFileConfig>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOpenVpnServer_Updates_Entity_And_Adds_Config_When_Missing()
    {
        var (svc, _, ip, _, serverQ, cfgQ, _, serverCmd, cfgCmd, quotaPlanCmd, tagCmd, _, _, _) = CreateService();
        var server = new OpenVpnServer { Id = 51, IsDefault = false, ServerName = "Srv" };

        cfgQ.Setup(q => q.AnyByVpnServerId(51, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("198.51.100.5");
        serverQ.Setup(q => q.AnyByServerNameExceptId("Srv", 51, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        cfgCmd.Setup(c => c.Add(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenVpnServerOvpnFileConfig e, bool _, CancellationToken _) => e);

        serverCmd.Setup(c => c.Update(It.IsAny<OpenVpnServer>(), true, It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));
        serverQ.Setup(q => q.GetById(51, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        quotaPlanCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<QuotaPlanAllowedServer, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
        tagCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<OpenVpnServerTag, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

        var before = DateTimeOffset.UtcNow;
        var result = await svc.UpdateOpenVpnServer(server, new List<int>(), new List<int>(), CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(51, result.Id);
        Assert.InRange(server.LastUpdate, before, after);
        cfgCmd.Verify(c => c.Add(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Once);
        serverCmd.Verify(c => c.Update(server, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOpenVpnServer_Unsets_Other_Defaults_When_IsDefault_True()
    {
        var (svc, _, ip, _, serverQ, cfgQ, _, serverCmd, _, quotaPlanCmd, tagCmd, _, _, _) = CreateService();
        var server = new OpenVpnServer { Id = 9, IsDefault = true, ServerName = "Srv" };

        cfgQ.Setup(q => q.AnyByVpnServerId(9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("ignored");
        serverQ.Setup(q => q.AnyByServerNameExceptId("Srv", 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        serverCmd.Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<OpenVpnServer>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        serverCmd.Setup(c => c.Update(It.IsAny<OpenVpnServer>(), true, It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));
        serverQ.Setup(q => q.GetById(9, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        quotaPlanCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<QuotaPlanAllowedServer, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
        tagCmd.Setup(c => c.DeleteWhere(It.IsAny<Expression<Func<OpenVpnServerTag, bool>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

        var result = await svc.UpdateOpenVpnServer(server, new List<int>(), new List<int>(), CancellationToken.None);
        Assert.Equal(9, result.Id);

        serverCmd.Verify(c => c.UpdateWhere(
                It.IsAny<Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<OpenVpnServer>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOpenVpnServer_Performs_SoftDelete_WithUpdateWhere()
    {
        var (svc, _, _, _, serverQ, _, _, serverCmd, _, _, _, _, _, _) = CreateService();
        var entity = new OpenVpnServer { Id = 77, ServerName = "Srv" };
        serverQ.Setup(q => q.GetById(77, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        serverCmd.Setup(c => c.UpdateWhere(
                It.IsAny<Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<OpenVpnServer>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        var ok = await svc.DeleteOpenVpnServer(77, CancellationToken.None);

        Assert.True(ok);
        serverCmd.Verify(c => c.UpdateWhere(
                It.IsAny<Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<Action<UpdateSettersBuilder<OpenVpnServer>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOpenVpnServer_Throws_When_NotFound()
    {
        var (svc, _, _, _, serverQ, _, _, _, _, _, _, _, _, _) = CreateService();
        serverQ.Setup(q => q.GetById(555, It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServer?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteOpenVpnServer(555, CancellationToken.None));
        Assert.Contains("not found", ex.Message);
    }
}
