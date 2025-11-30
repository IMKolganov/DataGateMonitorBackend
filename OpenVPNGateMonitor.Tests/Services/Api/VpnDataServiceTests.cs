using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;

namespace OpenVPNGateMonitor.Tests.Services.Api;

public class VpnDataServiceTests
{
    private static (VpnDataService svc,
        Mock<ILogger<IVpnDataService>> log,
        Mock<IExternalIpAddressService> ip,
        Mock<IOpenVpnServerQueryService> serverQ,
        Mock<IOpenVpnServerOvpnFileConfigQueryService> cfgQ,
        Mock<ITransactionRunner> trx,
        Mock<ICommandService<OpenVpnServer, int>> serverCmd,
        Mock<ICommandService<OpenVpnServerOvpnFileConfig, int>> cfgCmd)
        CreateService()
    {
        var log = new Mock<ILogger<IVpnDataService>>();
        var ip = new Mock<IExternalIpAddressService>(MockBehavior.Strict);
        var serverQ = new Mock<IOpenVpnServerQueryService>(MockBehavior.Strict);
        var cfgQ = new Mock<IOpenVpnServerOvpnFileConfigQueryService>(MockBehavior.Strict);
        var trx = new Mock<ITransactionRunner>(MockBehavior.Strict);
        var serverCmd = new Mock<ICommandService<OpenVpnServer, int>>(MockBehavior.Strict);
        var cfgCmd = new Mock<ICommandService<OpenVpnServerOvpnFileConfig, int>>(MockBehavior.Strict);

        // Transaction runner should invoke provided delegate immediately
        trx.Setup(t => t.RunAsync(It.IsAny<Func<CancellationToken, Task<OpenVpnServer>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<OpenVpnServer>>, CancellationToken>(async (f, ct) => await f(ct));
        trx.Setup(t => t.RunAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (f, ct) => await f(ct));

        var svc = new VpnDataService(
            log.Object,
            ip.Object,
            serverQ.Object,
            cfgQ.Object,
            trx.Object,
            serverCmd.Object,
            cfgCmd.Object);

        return (svc, log, ip, serverQ, cfgQ, trx, serverCmd, cfgCmd);
    }

    [Fact]
    public async Task AddOpenVpnServer_Adds_Config_When_None_Exists_And_Not_Default()
    {
        var (svc, _, ip, serverQ, cfgQ, _, serverCmd, cfgCmd) = CreateService();
        var server = new OpenVpnServer { Id = 0, IsDefault = false, ServerName = "Srv" };

        var before = DateTimeOffset.UtcNow;

        // Any returns false -> no config exists
        cfgQ.Setup(q => q.AnyByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // External IP
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>()))
          .ReturnsAsync("203.0.113.10");

        // AddAsync should set Id and dates
        serverCmd.Setup(c => c.AddAsync(server, true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServer, bool, CancellationToken>((s, _, __) => { s.Id = 101; })
            .ReturnsAsync((OpenVpnServer s, bool _, CancellationToken __) => s);

        // Config AddAsync capture
        OpenVpnServerOvpnFileConfig? addedCfg = null;
        cfgCmd.Setup(c => c.AddAsync(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()))
              .Callback<OpenVpnServerOvpnFileConfig, bool, CancellationToken>((e, _, __) => addedCfg = e)
              .ReturnsAsync((OpenVpnServerOvpnFileConfig e, bool _, CancellationToken __) => e);

        // Final snapshot
        serverQ.Setup(q => q.GetByIdAsync(101, It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => server);

        var result = await svc.AddOpenVpnServer(server, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(101, result.Id);
        Assert.InRange(server.CreateDate, before, after);
        Assert.InRange(server.LastUpdate, before, after);

        Assert.NotNull(addedCfg);
        Assert.Equal(101, addedCfg!.VpnServerId);
        Assert.Equal("203.0.113.10", addedCfg.VpnServerIp);

        cfgQ.Verify(q => q.AnyByVpnServerId(101, It.IsAny<CancellationToken>()), Times.Once);
        cfgCmd.Verify(c => c.AddAsync(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Once);
        serverCmd.Verify(c => c.AddAsync(server, true, It.IsAny<CancellationToken>()), Times.Once);
        serverQ.Verify(q => q.GetByIdAsync(101, It.IsAny<CancellationToken>()), Times.Once);
        ip.VerifyAll();
    }

    [Fact]
    public async Task AddOpenVpnServer_Unsets_Previous_Default_When_IsDefault_True()
    {
        var (svc, _, ip, serverQ, cfgQ, _, serverCmd, cfgCmd) = CreateService();
        var server = new OpenVpnServer { Id = 0, IsDefault = true, ServerName = "DefaultSrv" };

        cfgQ.Setup(q => q.AnyByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        // External IP never used because Any=true
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("ignored");

        // Bulk unset defaults called
        serverCmd.Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<SetPropertyCalls<OpenVpnServer>, SetPropertyCalls<OpenVpnServer>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        serverCmd.Setup(c => c.AddAsync(server, true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServer, bool, CancellationToken>((s, _, __) => s.Id = 7)
            .ReturnsAsync((OpenVpnServer s, bool _, CancellationToken __) => s);

        serverQ.Setup(q => q.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(() => server);

        var result = await svc.AddOpenVpnServer(server, CancellationToken.None);

        Assert.Equal(7, result.Id);

        serverCmd.Verify(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<SetPropertyCalls<OpenVpnServer>, SetPropertyCalls<OpenVpnServer>>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        // Because AnyByVpnServerId returned true, no config added
        cfgCmd.Verify(c => c.AddAsync(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOpenVpnServer_Updates_Entity_And_Adds_Config_When_Missing()
    {
        var (svc, _, ip, serverQ, cfgQ, _, serverCmd, cfgCmd) = CreateService();
        var server = new OpenVpnServer { Id = 51, IsDefault = false, ServerName = "Srv" };

        // Any=false -> will add config
        cfgQ.Setup(q => q.AnyByVpnServerId(51, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("198.51.100.5");
        cfgCmd.Setup(c => c.AddAsync(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()))
             .ReturnsAsync((OpenVpnServerOvpnFileConfig e, bool _, CancellationToken __) => e);

        serverCmd.Setup(c => c.UpdateAsync(server, true, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        serverQ.Setup(q => q.GetByIdAsync(51, It.IsAny<CancellationToken>())).ReturnsAsync(() => server);

        var before = DateTimeOffset.UtcNow;
        var result = await svc.UpdateOpenVpnServer(server, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(51, result.Id);
        Assert.InRange(server.LastUpdate, before, after);
        cfgCmd.Verify(c => c.AddAsync(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Once);
        serverCmd.Verify(c => c.UpdateAsync(server, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOpenVpnServer_Unsets_Other_Defaults_When_IsDefault_True()
    {
        var (svc, _, ip, serverQ, cfgQ, _, serverCmd, cfgCmd) = CreateService();
        var server = new OpenVpnServer { Id = 9, IsDefault = true, ServerName = "Srv" };

        cfgQ.Setup(q => q.AnyByVpnServerId(9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ip.Setup(x => x.GetRemoteIpAddress(It.IsAny<CancellationToken>())).ReturnsAsync("ignored");

        serverCmd.Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<SetPropertyCalls<OpenVpnServer>, SetPropertyCalls<OpenVpnServer>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        serverCmd.Setup(c => c.UpdateAsync(server, true, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        serverQ.Setup(q => q.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(() => server);

        var result = await svc.UpdateOpenVpnServer(server, CancellationToken.None);
        Assert.Equal(9, result.Id);
        cfgCmd.Verify(c => c.AddAsync(It.IsAny<OpenVpnServerOvpnFileConfig>(), true, It.IsAny<CancellationToken>()), Times.Never);
        serverCmd.Verify(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<SetPropertyCalls<OpenVpnServer>, SetPropertyCalls<OpenVpnServer>>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOpenVpnServer_Returns_True_When_Deleted()
    {
        var (svc, _, _, serverQ, _, _, serverCmd, _) = CreateService();
        var entity = new OpenVpnServer { Id = 77, ServerName = "Srv" };
        serverQ.Setup(q => q.GetByIdAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        serverCmd.Setup(c => c.DeleteAsync(entity, true, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var ok = await svc.DeleteOpenVpnServer(77, CancellationToken.None);
        Assert.True(ok);
        serverCmd.Verify(c => c.DeleteAsync(entity, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOpenVpnServer_Throws_When_NotFound()
    {
        var (svc, _, _, serverQ, _, _, _, _) = CreateService();
        serverQ.Setup(q => q.GetByIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServer?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteOpenVpnServer(555, CancellationToken.None));
        Assert.Contains("OpenVpnServer not found", ex.Message);
    }
}
