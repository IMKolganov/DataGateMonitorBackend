using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Hubs.BackgroundService;
using DataGateMonitor.Mapping.VpnServers.Mappings;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.Cache;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Hubs;
using DataGateMonitor.SharedModels.Enums;
using FluentAssertions;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Hubs.BackgroundService;

public class OpenVpnStatusStreamPublisherTests
{
    public OpenVpnStatusStreamPublisherTests()
    {
        new VpnServerMapping().Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Publish_OverwritesIsOnlineFromComposedFlags_AndProbeFromRawFlags()
    {
        var background = new Mock<IOpenVpnBackgroundService>();
        background.Setup(b => b.GetStatus()).Returns(new Dictionary<int, ServiceStatusDto>
        {
            [7] = new()
            {
                VpnServerId = 7,
                Status = ServiceStatus.Running,
                // Stale poller values — publisher must overwrite from DB flags.
                IsOnline = true,
                IsAvailableByExternalProbe = true,
            },
            [8] = new()
            {
                VpnServerId = 8,
                Status = ServiceStatus.Running,
                IsOnline = true,
                IsAvailableByExternalProbe = true,
            },
            [9] = new()
            {
                VpnServerId = 9,
                Status = ServiceStatus.Idle,
                IsOnline = false,
                IsAvailableByExternalProbe = false,
            },
        });

        var overview = new Mock<IVpnServerOverviewQuery>();
        overview
            .Setup(q => q.GetOnlineFlagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool>
            {
                // Manager online but externally blocked → composed offline.
                [7] = false,
                // Manager online + probe ok → online.
                [8] = true,
                // Missing 9 → GetValueOrDefault → false for IsOnline.
            });
        overview
            .Setup(q => q.GetExternalProbeFlagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool>
            {
                [7] = false,
                [8] = true,
                // Missing 9 → GetValueOrDefault(..., true) for probe flag.
            });
        overview
            .Setup(q => q.GetClientCountersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));

        var services = new ServiceCollection();
        services.AddSingleton(overview.Object);
        await using var provider = services.BuildServiceProvider();

        var counter = new Mock<IConnectedClientsCounterStore>();
        counter
            .Setup(c => c.GetManyAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        StatusStreamPayload? payload = null;
        var group = new Mock<IClientProxy>();
        group
            .Setup(c => c.SendCoreAsync("StatusUpdated", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
                payload = Assert.IsType<StatusStreamPayload>(args[0]))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group("status-stream")).Returns(group.Object);

        var hub = new Mock<IHubContext<OpenVpnStatusHub>>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);

        var sut = new OpenVpnStatusStreamPublisher(
            hub.Object,
            background.Object,
            provider.GetRequiredService<IServiceScopeFactory>(),
            counter.Object,
            NullLogger<OpenVpnStatusStreamPublisher>.Instance);

        using var cts = new CancellationTokenSource();
        await sut.StartAsync(cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (payload is null && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        await cts.CancelAsync();
        await sut.StopAsync(CancellationToken.None);

        payload.Should().NotBeNull();
        var byId = payload!.Statuses.ToDictionary(s => s.ServiceStatus.VpnServerId);

        byId[7].ServiceStatus.IsOnline.Should().BeFalse();
        byId[7].ServiceStatus.IsAvailableByExternalProbe.Should().BeFalse();

        byId[8].ServiceStatus.IsOnline.Should().BeTrue();
        byId[8].ServiceStatus.IsAvailableByExternalProbe.Should().BeTrue();

        // Missing from both dictionaries: Online defaults false, probe defaults true.
        byId[9].ServiceStatus.IsOnline.Should().BeFalse();
        byId[9].ServiceStatus.IsAvailableByExternalProbe.Should().BeTrue();
    }
}
