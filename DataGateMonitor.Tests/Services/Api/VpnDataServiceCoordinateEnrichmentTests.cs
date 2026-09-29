using Moq;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api;

public class VpnDataServiceCoordinateEnrichmentTests
{
    [Fact]
    public async Task AddVpnServer_CallsCoordinateEnricher_WithNullPreferredIp()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupInsertServer("geo-srv", 42);
        VpnServer? captured = null;
        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()))
            .Callback<VpnServer, string?, CancellationToken>((s, _, _) =>
            {
                s.Latitude = 35.18;
                s.Longitude = 33.38;
                captured = s;
            })
            .Returns(Task.CompletedTask);

        await h.Create().AddVpnServer(
            new VpnServer { ServerName = "geo-srv", ApiUrl = "https://203.0.113.5/" },
            [],
            [],
            CancellationToken.None);

        h.CoordinateEnricher.Verify(
            e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
        h.PublicIpLookup.Verify(
            p => p.GetAsync(It.IsAny<int>(), It.IsAny<VpnServerType>(), It.IsAny<CancellationToken>()),
            Times.Never);
        h.PublicIpLookup.Verify(p => p.TryGetCached(It.IsAny<int>()), Times.Never);
        Assert.NotNull(captured);
        Assert.Equal(35.18, captured!.Latitude);
        Assert.Equal(33.38, captured.Longitude);
    }

    [Fact]
    public async Task AddVpnServer_Continues_WhenCoordinateEnricherThrows()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupInsertServer("geo-srv", 42);
        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("geo down"));

        var result = await h.Create().AddVpnServer(
            new VpnServer { ServerName = "geo-srv", ApiUrl = "https://203.0.113.5/" },
            [],
            [],
            CancellationToken.None);

        Assert.Equal(42, result.Id);
    }

    [Fact]
    public async Task AddVpnServer_Continues_WhenCoordinateEnricherTimesOut()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupInsertServer("geo-srv", 42);
        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await h.Create().AddVpnServer(
            new VpnServer { ServerName = "geo-srv", ApiUrl = "https://203.0.113.5/" },
            [],
            [],
            CancellationToken.None);

        Assert.Equal(42, result.Id);
    }

    [Fact]
    public async Task AddVpnServer_StillCallsEnricher_WhenCoordsAlreadyPresent()
    {
        var h = new VpnDataServiceTestHarness();
        h.SetupInsertServer("geo-srv", 7);
        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await h.Create().AddVpnServer(
            new VpnServer
            {
                ServerName = "geo-srv",
                ApiUrl = "https://203.0.113.5/",
                Latitude = 1,
                Longitude = 2
            },
            [],
            [],
            CancellationToken.None);

        h.CoordinateEnricher.Verify(
            e => e.TryFillMissingAsync(
                It.Is<VpnServer>(s => s.Latitude == 1 && s.Longitude == 2),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateVpnServer_SkipsEnricher_WhenCoordinatesAlreadySet()
    {
        var h = new VpnDataServiceTestHarness();
        h.CfgQ.Setup(q => q.AnyByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv5", 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        h.ServerQ.SetupSequence(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer
            {
                Id = 5,
                ServerName = "srv5",
                ApiUrl = "https://old.example/",
                Latitude = 10,
                Longitude = 20
            })
            .ReturnsAsync(new VpnServer
            {
                Id = 5,
                ServerName = "srv5",
                ApiUrl = "https://old.example/",
                Latitude = 10,
                Longitude = 20
            });

        await h.Create().UpdateVpnServer(
            new VpnServer
            {
                Id = 5,
                ServerName = "srv5",
                ApiUrl = "https://old.example/",
                Latitude = 10,
                Longitude = 20
            },
            [],
            [],
            CancellationToken.None);

        h.CoordinateEnricher.Verify(
            e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        h.PublicIpLookup.Verify(p => p.TryGetCached(It.IsAny<int>()), Times.Never);
        h.PublicIpLookup.Verify(
            p => p.GetAsync(It.IsAny<int>(), It.IsAny<VpnServerType>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(10.0, null)]
    [InlineData(null, 20.0)]
    public async Task UpdateVpnServer_SkipsEnricher_WhenOnlyOneCoordinateSet(double? lat, double? lon)
    {
        var h = new VpnDataServiceTestHarness();
        h.CfgQ.Setup(q => q.AnyByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv5", 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        h.ServerQ.SetupSequence(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/" })
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/", Latitude = lat, Longitude = lon });

        await h.Create().UpdateVpnServer(
            new VpnServer
            {
                Id = 5,
                ServerName = "srv5",
                ApiUrl = "https://a/",
                Latitude = lat,
                Longitude = lon
            },
            [],
            [],
            CancellationToken.None);

        h.CoordinateEnricher.Verify(
            e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateVpnServer_UsesCachedPublicIpOnly_WhenApiUrlUnchangedAndCoordsMissing()
    {
        var h = new VpnDataServiceTestHarness();
        h.CfgQ.Setup(q => q.AnyByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv5", 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        h.ServerQ.SetupSequence(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer
            {
                Id = 5,
                ServerName = "srv5",
                ApiUrl = "https://same.example/",
                ServerType = VpnServerType.OpenVpn
            })
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://same.example/" });

        h.PublicIpLookup.Setup(p => p.TryGetCached(5)).Returns("203.0.113.77");
        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), "203.0.113.77", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await h.Create().UpdateVpnServer(
            new VpnServer
            {
                Id = 5,
                ServerName = "srv5",
                ApiUrl = "https://same.example/",
                ServerType = VpnServerType.OpenVpn
            },
            [],
            [],
            CancellationToken.None);

        h.PublicIpLookup.Verify(p => p.TryGetCached(5), Times.Once);
        h.PublicIpLookup.Verify(
            p => p.GetAsync(It.IsAny<int>(), It.IsAny<VpnServerType>(), It.IsAny<CancellationToken>()),
            Times.Never);
        h.CoordinateEnricher.Verify(
            e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), "203.0.113.77", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateVpnServer_SkipsCachedPublicIp_WhenApiUrlChanged()
    {
        var h = new VpnDataServiceTestHarness();
        h.CfgQ.Setup(q => q.AnyByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv5", 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        h.ServerQ.SetupSequence(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://old.example/" })
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://new.example/" });

        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://new.example/" },
            [],
            [],
            CancellationToken.None);

        h.PublicIpLookup.Verify(p => p.TryGetCached(It.IsAny<int>()), Times.Never);
        h.PublicIpLookup.Verify(
            p => p.GetAsync(It.IsAny<int>(), It.IsAny<VpnServerType>(), It.IsAny<CancellationToken>()),
            Times.Never);
        h.CoordinateEnricher.Verify(
            e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
        h.PublicIpLookup.Verify(p => p.Invalidate(5), Times.Once);
    }

    [Fact]
    public async Task UpdateVpnServer_Continues_WhenCoordinateEnricherThrows()
    {
        var h = new VpnDataServiceTestHarness();
        h.CfgQ.Setup(q => q.AnyByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv5", 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        h.ServerQ.SetupSequence(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/" })
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/" });

        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("geo down"));

        var result = await h.Create().UpdateVpnServer(
            new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/" },
            [],
            [],
            CancellationToken.None);

        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task UpdateVpnServer_PassesNullPreferredIp_WhenCacheMiss()
    {
        var h = new VpnDataServiceTestHarness();
        h.CfgQ.Setup(q => q.AnyByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        h.ServerQ.Setup(q => q.AnyByServerNameExceptId("srv5", 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        h.ServerCmd.Setup(c => c.Update(It.IsAny<VpnServer>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        h.ServerQ.SetupSequence(q => q.GetById(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/" })
            .ReturnsAsync(new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/" });

        h.PublicIpLookup.Setup(p => p.TryGetCached(5)).Returns((string?)null);
        h.CoordinateEnricher
            .Setup(e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await h.Create().UpdateVpnServer(
            new VpnServer { Id = 5, ServerName = "srv5", ApiUrl = "https://a/" },
            [],
            [],
            CancellationToken.None);

        h.PublicIpLookup.Verify(
            p => p.GetAsync(It.IsAny<int>(), It.IsAny<VpnServerType>(), It.IsAny<CancellationToken>()),
            Times.Never);
        h.CoordinateEnricher.Verify(
            e => e.TryFillMissingAsync(It.IsAny<VpnServer>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
