using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class VpnServerConflogServiceTests
{
    [Fact]
    public async Task FetchAndSaveIfChangedAsync_WhenNoLastRecord_SavesAndReturnsEntity()
    {
        var response = new VpnMicroserviceDiagnosticsDto
        {
            ServerType = VpnServerType.OpenVpn,
            OpenVpn = new RootInfoResponse(),
        };
        var micro = new Mock<IMicroserviceInfoService>();
        micro.Setup(m => m.GetInfoByUrlAsync("https://host", VpnServerType.OpenVpn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var serverQ = new Mock<IVpnServerQueryService>();
        serverQ.Setup(q => q.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 1, ServerType = VpnServerType.OpenVpn, ApiUrl = "https://host" });

        var conflogQ = new Mock<IVpnServerConflogQueryService>();
        conflogQ.Setup(q => q.GetLastByVpnServerId(1, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServerConflog?)null);
        conflogQ.Setup(q => q.GetLastByRequestUrl("https://host", It.IsAny<CancellationToken>())).ReturnsAsync((VpnServerConflog?)null);

        VpnServerConflog? added = null;
        var command = new Mock<ICommandService<VpnServerConflog, int>>();
        command.Setup(c => c.Add(It.IsAny<VpnServerConflog>(), true, It.IsAny<CancellationToken>()))
            .Callback<VpnServerConflog, bool, CancellationToken>((e, _, _) => added = e)
            .ReturnsAsync((VpnServerConflog e, bool _, CancellationToken _) => { e.Id = 10; return e; });

        var sut = new VpnServerConflogService(micro.Object, conflogQ.Object, command.Object, serverQ.Object);
        var result = await sut.FetchAndSaveIfChangedAsync("https://host", 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(added);
        Assert.Equal(1, added!.VpnServerId);
        Assert.Equal("https://host", added.RequestUrl);
        Assert.NotNull(added.PayloadJson);
        command.Verify(c => c.Add(It.IsAny<VpnServerConflog>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchAndSaveIfChangedAsync_WhenLastSamePayload_ReturnsNull()
    {
        var response = new VpnMicroserviceDiagnosticsDto
        {
            ServerType = VpnServerType.OpenVpn,
            OpenVpn = new RootInfoResponse(),
        };
        var micro = new Mock<IMicroserviceInfoService>();
        micro.Setup(m => m.GetInfoByUrlAsync("https://host", null, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, WriteIndented = false };
        var existingJson = System.Text.Json.JsonSerializer.Serialize(response, jsonOptions);
        var last = new VpnServerConflog { Id = 1, RequestUrl = "https://host", PayloadJson = existingJson };
        var conflogQ = new Mock<IVpnServerConflogQueryService>();
        conflogQ.Setup(q => q.GetLastByRequestUrl(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(last);

        var command = new Mock<ICommandService<VpnServerConflog, int>>();

        var sut = new VpnServerConflogService(micro.Object, conflogQ.Object, command.Object, Mock.Of<IVpnServerQueryService>());
        var result = await sut.FetchAndSaveIfChangedAsync("https://host", null, CancellationToken.None);

        Assert.Null(result);
        command.Verify(c => c.Add(It.IsAny<VpnServerConflog>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FetchAndSaveIfChangedByServerIdAsync_WhenServerNotFound_Throws()
    {
        var serverQ = new Mock<IVpnServerQueryService>();
        serverQ.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServer?)null);

        var sut = new VpnServerConflogService(
            Mock.Of<IMicroserviceInfoService>(),
            Mock.Of<IVpnServerConflogQueryService>(),
            Mock.Of<ICommandService<VpnServerConflog, int>>(),
            serverQ.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.FetchAndSaveIfChangedByServerIdAsync(99, CancellationToken.None));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task FetchAndSaveIfChangedByServerIdAsync_WhenApiUrlEmpty_Throws()
    {
        var server = new VpnServer { Id = 5, ServerName = "S", ApiUrl = "" };
        var serverQ = new Mock<IVpnServerQueryService>();
        serverQ.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        var sut = new VpnServerConflogService(
            Mock.Of<IMicroserviceInfoService>(),
            Mock.Of<IVpnServerConflogQueryService>(),
            Mock.Of<ICommandService<VpnServerConflog, int>>(),
            serverQ.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.FetchAndSaveIfChangedByServerIdAsync(5, CancellationToken.None));
        Assert.Contains("ApiUrl", ex.Message);
    }

    /// <summary>
    /// After server recreate (delete + add): new server has new Id, no conflog by VpnServerId.
    /// Old conflog by same RequestUrl may still exist (from deleted server). We must save a new record
    /// for the new server and must NOT use the old record to skip saving.
    /// </summary>
    [Fact]
    public async Task FetchAndSaveIfChangedAsync_WhenServerRecreated_WithOldConflogBySameUrl_StillSavesNewRecord()
    {
        var response = new VpnMicroserviceDiagnosticsDto
        {
            ServerType = VpnServerType.OpenVpn,
            OpenVpn = new RootInfoResponse(),
        };
        var micro = new Mock<IMicroserviceInfoService>();
        micro.Setup(m => m.GetInfoByUrlAsync("https://host", VpnServerType.OpenVpn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var serverQ = new Mock<IVpnServerQueryService>();
        serverQ.Setup(q => q.GetById(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 10, ServerType = VpnServerType.OpenVpn, ApiUrl = "https://host" });

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, WriteIndented = false };
        var samePayloadJson = System.Text.Json.JsonSerializer.Serialize(response, jsonOptions);
        var oldConflogFromDeletedServer = new VpnServerConflog { Id = 1, VpnServerId = 5, RequestUrl = "https://host", PayloadJson = samePayloadJson };

        var conflogQ = new Mock<IVpnServerConflogQueryService>();
        conflogQ.Setup(q => q.GetLastByVpnServerId(10, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServerConflog?)null);
        conflogQ.Setup(q => q.GetLastByRequestUrl("https://host", It.IsAny<CancellationToken>())).ReturnsAsync(oldConflogFromDeletedServer);

        VpnServerConflog? added = null;
        var command = new Mock<ICommandService<VpnServerConflog, int>>();
        command.Setup(c => c.Add(It.IsAny<VpnServerConflog>(), true, It.IsAny<CancellationToken>()))
            .Callback<VpnServerConflog, bool, CancellationToken>((e, _, _) => added = e)
            .ReturnsAsync((VpnServerConflog e, bool _, CancellationToken _) => { e.Id = 100; return e; });

        var sut = new VpnServerConflogService(micro.Object, conflogQ.Object, command.Object, serverQ.Object);
        var result = await sut.FetchAndSaveIfChangedAsync("https://host", 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(added);
        Assert.Equal(10, added!.VpnServerId);
        Assert.Equal("https://host", added.RequestUrl);
        command.Verify(c => c.Add(It.IsAny<VpnServerConflog>(), true, It.IsAny<CancellationToken>()), Times.Once);
        conflogQ.Verify(q => q.GetLastByRequestUrl(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
