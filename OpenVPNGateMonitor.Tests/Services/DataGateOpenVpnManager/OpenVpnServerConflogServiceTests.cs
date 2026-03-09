using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerConflogTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class OpenVpnServerConflogServiceTests
{
    [Fact]
    public async Task FetchAndSaveIfChangedAsync_WhenNoLastRecord_SavesAndReturnsEntity()
    {
        var response = new RootInfoResponse();
        var micro = new Mock<IMicroserviceInfoService>();
        micro.Setup(m => m.GetInfoByUrlAsync("https://host", It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var conflogQ = new Mock<IOpenVpnServerConflogQueryService>();
        conflogQ.Setup(q => q.GetLastByVpnServerId(1, It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServerConflog?)null);
        conflogQ.Setup(q => q.GetLastByRequestUrl("https://host", It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServerConflog?)null);

        OpenVpnServerConflog? added = null;
        var command = new Mock<ICommandService<OpenVpnServerConflog, int>>();
        command.Setup(c => c.Add(It.IsAny<OpenVpnServerConflog>(), true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServerConflog, bool, CancellationToken>((e, _, _) => added = e)
            .ReturnsAsync((OpenVpnServerConflog e, bool _, CancellationToken _) => { e.Id = 10; return e; });

        var sut = new OpenVpnServerConflogService(micro.Object, conflogQ.Object, command.Object, Mock.Of<IOpenVpnServerQueryService>());
        var result = await sut.FetchAndSaveIfChangedAsync("https://host", 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(added);
        Assert.Equal(1, added!.VpnServerId);
        Assert.Equal("https://host", added.RequestUrl);
        Assert.NotNull(added.PayloadJson);
        command.Verify(c => c.Add(It.IsAny<OpenVpnServerConflog>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchAndSaveIfChangedAsync_WhenLastSamePayload_ReturnsNull()
    {
        var response = new RootInfoResponse();
        var micro = new Mock<IMicroserviceInfoService>();
        micro.Setup(m => m.GetInfoByUrlAsync("https://host", It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, WriteIndented = false };
        var existingJson = System.Text.Json.JsonSerializer.Serialize(response, jsonOptions);
        var last = new OpenVpnServerConflog { Id = 1, RequestUrl = "https://host", PayloadJson = existingJson };
        var conflogQ = new Mock<IOpenVpnServerConflogQueryService>();
        conflogQ.Setup(q => q.GetLastByRequestUrl(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(last);

        var command = new Mock<ICommandService<OpenVpnServerConflog, int>>();

        var sut = new OpenVpnServerConflogService(micro.Object, conflogQ.Object, command.Object, Mock.Of<IOpenVpnServerQueryService>());
        var result = await sut.FetchAndSaveIfChangedAsync("https://host", null, CancellationToken.None);

        Assert.Null(result);
        command.Verify(c => c.Add(It.IsAny<OpenVpnServerConflog>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FetchAndSaveIfChangedByServerIdAsync_WhenServerNotFound_Throws()
    {
        var serverQ = new Mock<IOpenVpnServerQueryService>();
        serverQ.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServer?)null);

        var sut = new OpenVpnServerConflogService(
            Mock.Of<IMicroserviceInfoService>(),
            Mock.Of<IOpenVpnServerConflogQueryService>(),
            Mock.Of<ICommandService<OpenVpnServerConflog, int>>(),
            serverQ.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.FetchAndSaveIfChangedByServerIdAsync(99, CancellationToken.None));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task FetchAndSaveIfChangedByServerIdAsync_WhenApiUrlEmpty_Throws()
    {
        var server = new OpenVpnServer { Id = 5, ServerName = "S", ApiUrl = "" };
        var serverQ = new Mock<IOpenVpnServerQueryService>();
        serverQ.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        var sut = new OpenVpnServerConflogService(
            Mock.Of<IMicroserviceInfoService>(),
            Mock.Of<IOpenVpnServerConflogQueryService>(),
            Mock.Of<ICommandService<OpenVpnServerConflog, int>>(),
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
        var response = new RootInfoResponse();
        var micro = new Mock<IMicroserviceInfoService>();
        micro.Setup(m => m.GetInfoByUrlAsync("https://host", It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, WriteIndented = false };
        var samePayloadJson = System.Text.Json.JsonSerializer.Serialize(response, jsonOptions);
        var oldConflogFromDeletedServer = new OpenVpnServerConflog { Id = 1, VpnServerId = 5, RequestUrl = "https://host", PayloadJson = samePayloadJson };

        var conflogQ = new Mock<IOpenVpnServerConflogQueryService>();
        conflogQ.Setup(q => q.GetLastByVpnServerId(10, It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServerConflog?)null);
        conflogQ.Setup(q => q.GetLastByRequestUrl("https://host", It.IsAny<CancellationToken>())).ReturnsAsync(oldConflogFromDeletedServer);

        OpenVpnServerConflog? added = null;
        var command = new Mock<ICommandService<OpenVpnServerConflog, int>>();
        command.Setup(c => c.Add(It.IsAny<OpenVpnServerConflog>(), true, It.IsAny<CancellationToken>()))
            .Callback<OpenVpnServerConflog, bool, CancellationToken>((e, _, _) => added = e)
            .ReturnsAsync((OpenVpnServerConflog e, bool _, CancellationToken _) => { e.Id = 100; return e; });

        var sut = new OpenVpnServerConflogService(micro.Object, conflogQ.Object, command.Object, Mock.Of<IOpenVpnServerQueryService>());
        var result = await sut.FetchAndSaveIfChangedAsync("https://host", 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(added);
        Assert.Equal(10, added!.VpnServerId);
        Assert.Equal("https://host", added.RequestUrl);
        command.Verify(c => c.Add(It.IsAny<OpenVpnServerConflog>(), true, It.IsAny<CancellationToken>()), Times.Once);
        conflogQ.Verify(q => q.GetLastByRequestUrl(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
