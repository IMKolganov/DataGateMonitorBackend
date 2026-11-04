using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.Api;

public class VpnServerPiHoleProvisioningServiceTests
{
    [Theory]
    [InlineData(VpnServerDiscoveryStatus.Approved, VpnServerType.OpenVpn)]
    [InlineData(VpnServerDiscoveryStatus.Pending, VpnServerType.OpenVpn)]
    [InlineData(VpnServerDiscoveryStatus.Approved, VpnServerType.Xray)]
    public async Task ProvisionAsync_ImportsNodeCredentials_AppliesRuntime_AndClearsStagedSecret(
        VpnServerDiscoveryStatus status, VpnServerType serverType)
    {
        await using var db = CreateContext();
        Seed(db, status, serverType);
        var service = CreateService(db, out var config);

        Assert.True(await service.ProvisionAsync(1, CancellationToken.None));

        var saved = await db.VpnServerPiHoleConfigs.SingleAsync();
        Assert.Equal(PiHoleTestFixtures.AppCredential, saved.AppPassword);
        Assert.Equal(serverType == VpnServerType.Xray ? "10.80.1." : "10.51.40.", saved.ClientSubnetPrefix);
        config.Verify(c => c.ApplyRuntimeToMicroserviceAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null((await db.VpnServerDiscoveries.SingleAsync()).PiHoleConfigJson);
    }

    [Fact]
    public async Task ProvisionAsync_PreservesCompleteManualConfiguration()
    {
        await using var db = CreateContext();
        Seed(db);
        db.VpnServerPiHoleConfigs.Add(new VpnServerPiHoleConfig
        {
            VpnServerId = 1, BaseUrl = "http://manual-pihole:8080", AppPassword = "manual-credential"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, out var config);

        Assert.True(await service.ProvisionAsync(1, CancellationToken.None));

        config.Verify(c => c.UpsertAsync(It.IsAny<UpsertVpnServerPiHoleConfigRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("manual-credential", (await db.VpnServerPiHoleConfigs.SingleAsync()).AppPassword);
    }

    [Fact]
    public async Task ProvisionAsync_FillsMissingPasswordOnlyForMatchingPiHoleUrl()
    {
        await using var db = CreateContext();
        Seed(db);
        db.VpnServerPiHoleConfigs.Add(new VpnServerPiHoleConfig { VpnServerId = 1, BaseUrl = "http://10.51.40.1:8080/" });
        await db.SaveChangesAsync();
        var service = CreateService(db, out var config);

        Assert.True(await service.ProvisionAsync(1, CancellationToken.None));

        config.Verify(c => c.UpsertAsync(It.IsAny<UpsertVpnServerPiHoleConfigRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("rejected")]
    [InlineData("different-node")]
    [InlineData("different-stack")]
    [InlineData("manual-url")]
    public async Task ProvisionAsync_DoesNotImportUnrelatedOrDisabledReports(string scenario)
    {
        await using var db = CreateContext();
        Seed(db);
        switch (scenario)
        {
            case "disabled": (await db.VpnServers.SingleAsync()).IsPiHoleEnabled = false; break;
            case "rejected": (await db.VpnServerDiscoveries.SingleAsync()).Status = VpnServerDiscoveryStatus.Rejected; break;
            case "different-node": (await db.VpnServerDiscoveries.SingleAsync()).ApiUrl = "https://other.example.com/"; break;
            case "different-stack": (await db.VpnServerDiscoveries.SingleAsync()).ServerType = VpnServerType.Xray; break;
            case "manual-url": db.VpnServerPiHoleConfigs.Add(new VpnServerPiHoleConfig { VpnServerId = 1, BaseUrl = "http://other-pihole" }); break;
        }
        await db.SaveChangesAsync();
        var service = CreateService(db, out var config);

        Assert.False(await service.ProvisionAsync(1, CancellationToken.None));

        config.Verify(c => c.UpsertAsync(It.IsAny<UpsertVpnServerPiHoleConfigRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        config.Verify(c => c.ApplyRuntimeToMicroserviceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_RetriesRuntimeFailure_WithoutLosingImportedCredentials()
    {
        await using var db = CreateContext();
        Seed(db);
        var service = CreateService(db, out var config);
        config.SetupSequence(c => c.ApplyRuntimeToMicroserviceAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Node starting"))
            .Returns(Task.CompletedTask);

        Assert.True(await service.ProvisionAsync(1, CancellationToken.None));
        Assert.Equal(PiHoleTestFixtures.AppCredential, (await db.VpnServerPiHoleConfigs.SingleAsync()).AppPassword);
        config.Verify(c => c.UpsertAsync(It.IsAny<UpsertVpnServerPiHoleConfigRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        config.Verify(c => c.ApplyRuntimeToMicroserviceAsync(1, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static VpnServerPiHoleProvisioningService CreateService(ApplicationDbContext db,
        out Mock<IVpnServerPiHoleConfigService> config)
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<VpnServer>()).Returns(new TestQuery<VpnServer>(db.VpnServers));
        uow.Setup(u => u.GetQuery<VpnServerDiscovery>()).Returns(new TestQuery<VpnServerDiscovery>(db.VpnServerDiscoveries));
        uow.Setup(u => u.GetQuery<VpnServerPiHoleConfig>()).Returns(new TestQuery<VpnServerPiHoleConfig>(db.VpnServerPiHoleConfigs));
        config = new Mock<IVpnServerPiHoleConfigService>(MockBehavior.Strict);
        config.Setup(c => c.UpsertAsync(It.IsAny<UpsertVpnServerPiHoleConfigRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpsertVpnServerPiHoleConfigRequest request, CancellationToken _) =>
            {
                var row = db.VpnServerPiHoleConfigs.FirstOrDefault(c => c.VpnServerId == request.VpnServerId);
                if (row is null)
                {
                    row = new VpnServerPiHoleConfig { VpnServerId = request.VpnServerId };
                    db.VpnServerPiHoleConfigs.Add(row);
                }
                row.AppPassword = request.AppPassword!;
                row.BaseUrl = request.BaseUrl;
                row.ClientSubnetPrefix = request.ClientSubnetPrefix!;
                db.SaveChanges();
                return new VpnServerPiHoleConfigResponse();
            });
        config.Setup(c => c.ApplyRuntimeToMicroserviceAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var command = new Mock<ICommandService<VpnServerDiscovery, int>>();
        command.Setup(c => c.SaveChanges(It.IsAny<CancellationToken>())).Returns((CancellationToken ct) => db.SaveChangesAsync(ct));
        return new VpnServerPiHoleProvisioningService(
            new EfQueryService<VpnServer, int>(uow.Object), new EfQueryService<VpnServerDiscovery, int>(uow.Object),
            new EfQueryService<VpnServerPiHoleConfig, int>(uow.Object), command.Object, config.Object,
            NullLogger<VpnServerPiHoleProvisioningService>.Instance);
    }

    private static void Seed(ApplicationDbContext db, VpnServerDiscoveryStatus status = VpnServerDiscoveryStatus.Approved,
        VpnServerType serverType = VpnServerType.OpenVpn)
    {
        db.VpnServers.Add(new VpnServer
        {
            Id = 1, ServerName = "test-node", ApiUrl = "https://vpn.example.com/", IsPiHoleEnabled = true, ServerType = serverType
        });
        db.VpnServerDiscoveries.Add(new VpnServerDiscovery
        {
            ApiUrl = "https://vpn.example.com/", Status = status, HasPiHole = true, ServerType = serverType,
            ResolvedVpnServerId = status == VpnServerDiscoveryStatus.Pending ? null : 1,
            PiHoleConfigJson = ProjectJson.Serialize(new AnnouncePiHoleConfig
            {
                BaseUrl = "http://10.51.40.1:8080", AppPassword = PiHoleTestFixtures.AppCredential,
                ClientSubnetPrefix = serverType == VpnServerType.Xray ? "10.80.1." : "10.51.40."
            })
        });
        db.SaveChanges();
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new ConfigurationBuilder().Build());
}
