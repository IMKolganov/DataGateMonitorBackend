using DataGateMonitor.Models;
using DataGateMonitor.Tests.Services.Api;
using Moq;

namespace DataGateMonitor.Tests.Services.Api;

public class VpnDataServicePresenceAndPublicIpCacheTests
{
    [Fact]
    public async Task DeleteVpnServer_MarksClientsDisconnected_AndInvalidatesPublicIpCache()
    {
        var h = new VpnDataServiceTestHarness();
        h.ServerQ.Setup(q => q.GetById(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 9, ServerName = "gone", IsOnline = true });
        h.ServerCmd.Setup(c => c.UpdateWhere(
                It.IsAny<System.Linq.Expressions.Expression<Func<VpnServer, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<VpnServer>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        h.Presence.Setup(p => p.MarkAllDisconnectedAsync(9, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var ok = await h.Create().DeleteVpnServer(9, CancellationToken.None);

        Assert.True(ok);
        h.ServerCmd.Verify(
            c => c.UpdateWhere(
                It.IsAny<System.Linq.Expressions.Expression<Func<VpnServer, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<VpnServer>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(9, It.IsAny<CancellationToken>()), Times.Once);
        h.PublicIpLookup.Verify(p => p.Invalidate(9), Times.Once);
        h.MicroserviceFactory.Verify(f => f.Invalidate(9), Times.Once);
        h.EventFactory.Verify(f => f.Remove(9), Times.Once);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenBecomesDeleted_ClearsOnline_AndMarksClientsDisconnected()
    {
        var h = new VpnDataServiceTestHarness();
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv9", 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.CfgQ.Setup(q => q.AnyByVpnServerId(9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.SetupSequence(q => q.GetById(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 9, ServerName = "srv9", IsOnline = true, IsDeleted = false })
            .ReturnsAsync(new VpnServer { Id = 9, ServerName = "srv9", IsOnline = false, IsDeleted = true });
        VpnServer? saved = null;
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Callback<VpnServer, bool, CancellationToken>((s, _, _) => saved = s)
            .Returns(Task.FromResult(1));
        h.Presence.Setup(p => p.MarkAllDisconnectedAsync(9, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 9, ServerName = "srv9", IsDeleted = true, IsOnline = true, IsDefault = true },
            [],
            [],
            CancellationToken.None);

        Assert.NotNull(saved);
        Assert.True(saved!.IsDeleted);
        Assert.False(saved.IsOnline);
        Assert.False(saved.IsDefault);
        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenRestored_ClearsDeleted_WithoutDisconnect()
    {
        var h = new VpnDataServiceTestHarness();
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv10", 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.CfgQ.Setup(q => q.AnyByVpnServerId(10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.SetupSequence(q => q.GetById(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 10, ServerName = "srv10", IsOnline = false, IsDeleted = true })
            .ReturnsAsync(new VpnServer { Id = 10, ServerName = "srv10", IsOnline = false, IsDeleted = false });
        VpnServer? saved = null;
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Callback<VpnServer, bool, CancellationToken>((s, _, _) => saved = s)
            .Returns(Task.FromResult(1));

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 10, ServerName = "srv10", IsDeleted = false },
            [],
            [],
            CancellationToken.None);

        Assert.NotNull(saved);
        Assert.False(saved!.IsDeleted);
        Assert.False(saved.IsOnline);
        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenRestoredNameTaken_Throws()
    {
        var h = new VpnDataServiceTestHarness();
        h.ServerQ.Setup(q => q.GetById(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 11, ServerName = "taken", IsDeleted = true });
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("taken", 11, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Create().UpdateVpnServer(
                new VpnServer { Id = 11, ServerName = "taken", IsDeleted = false },
                [],
                [],
                CancellationToken.None));

        Assert.Contains("same name", ex.Message);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenBecomesDisabled_MarksClientsDisconnected()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupUpdateServer(8, "srv8");
        h.ServerQ.SetupSequence(q => q.GetById(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 8, ServerName = "srv8", IsDisable = false, ApiUrl = "https://a/" })
            .ReturnsAsync(new VpnServer { Id = 8, ServerName = "srv8", IsDisable = true, ApiUrl = "https://a/" });
        h.CfgQ.Setup(q => q.AnyByVpnServerId(8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.Presence.Setup(p => p.MarkAllDisconnectedAsync(8, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 8, ServerName = "srv8", IsDisable = true, ApiUrl = "https://a/" },
            [],
            [],
            CancellationToken.None);

        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(8, It.IsAny<CancellationToken>()), Times.Once);
        h.PublicIpLookup.Verify(p => p.Invalidate(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenApiUrlChanges_InvalidatesPublicIpCache_WithoutDisconnectIfStillEnabled()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupUpdateServer(5, "srv5");
        h.ServerQ.SetupSequence(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", IsDisable = false, ApiUrl = "https://old/" })
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", IsDisable = false, ApiUrl = "https://new/" });
        h.CfgQ.Setup(q => q.AnyByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 5, ServerName = "srv5", IsDisable = false, ApiUrl = "https://new/" },
            [],
            [],
            CancellationToken.None);

        h.PublicIpLookup.Verify(p => p.Invalidate(5), Times.Once);
        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenBecomesEnabled_DoesNotMarkClientsDisconnected()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupUpdateServer(3, "srv3");
        h.ServerQ.SetupSequence(q => q.GetById(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 3, ServerName = "srv3", IsDisable = true, ApiUrl = "https://a/" })
            .ReturnsAsync(new VpnServer { Id = 3, ServerName = "srv3", IsDisable = false, ApiUrl = "https://a/" });
        h.CfgQ.Setup(q => q.AnyByVpnServerId(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 3, ServerName = "srv3", IsDisable = false, ApiUrl = "https://a/" },
            [],
            [],
            CancellationToken.None);

        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        h.PublicIpLookup.Verify(p => p.Invalidate(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenAlreadyDisabled_DoesNotMarkClientsDisconnectedAgain()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupUpdateServer(4, "srv4");
        h.ServerQ.SetupSequence(q => q.GetById(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 4, ServerName = "srv4", IsDisable = true, ApiUrl = "https://a/" })
            .ReturnsAsync(new VpnServer { Id = 4, ServerName = "srv4", IsDisable = true, ApiUrl = "https://a/" });
        h.CfgQ.Setup(q => q.AnyByVpnServerId(4, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 4, ServerName = "srv4", IsDisable = true, ApiUrl = "https://a/" },
            [],
            [],
            CancellationToken.None);

        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenDisableAndApiUrlChange_MarksDisconnected_AndInvalidatesPublicIp()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupUpdateServer(6, "srv6");
        h.ServerQ.SetupSequence(q => q.GetById(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 6, ServerName = "srv6", IsDisable = false, ApiUrl = "https://old/" })
            .ReturnsAsync(new VpnServer { Id = 6, ServerName = "srv6", IsDisable = true, ApiUrl = "https://new/" });
        h.CfgQ.Setup(q => q.AnyByVpnServerId(6, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.Presence.Setup(p => p.MarkAllDisconnectedAsync(6, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 6, ServerName = "srv6", IsDisable = true, ApiUrl = "https://new/" },
            [],
            [],
            CancellationToken.None);

        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(6, It.IsAny<CancellationToken>()), Times.Once);
        h.PublicIpLookup.Verify(p => p.Invalidate(6), Times.Once);
    }

    [Fact]
    public async Task UpdateVpnServer_WhenApiUrlDiffersOnlyByCaseOrWhitespace_DoesNotInvalidate()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupUpdateServer(15, "srv15");
        h.ServerQ.SetupSequence(q => q.GetById(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 15, ServerName = "srv15", IsDisable = false, ApiUrl = "https://A.example/" })
            .ReturnsAsync(new VpnServer { Id = 15, ServerName = "srv15", IsDisable = false, ApiUrl = "  HTTPS://a.example/  " });
        h.CfgQ.Setup(q => q.AnyByVpnServerId(15, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 15, ServerName = "srv15", IsDisable = false, ApiUrl = "  HTTPS://a.example/  " },
            [],
            [],
            CancellationToken.None);

        h.PublicIpLookup.Verify(p => p.Invalidate(It.IsAny<int>()), Times.Never);
        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteVpnServer_WhenNotFound_Throws_WithoutPresenceOrInvalidate()
    {
        var h = new VpnDataServiceTestHarness();
        h.ServerQ.Setup(q => q.GetById(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServer?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Create().DeleteVpnServer(404, CancellationToken.None));

        h.Presence.Verify(p => p.MarkAllDisconnectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        h.PublicIpLookup.Verify(p => p.Invalidate(It.IsAny<int>()), Times.Never);
    }
}
