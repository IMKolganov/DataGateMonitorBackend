using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Responses;
using DataGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateMonitor.Tests.Controllers;

public class VpnServerConflogControllerTests
{
    private readonly Mock<IVpnServerConflogService> _conflogService = new();
    private readonly Mock<IVpnServerConflogQueryService> _conflogQuery = new();
    private readonly VpnServerConflogController _controller;

    public VpnServerConflogControllerTests()
    {
        _controller = new VpnServerConflogController(_conflogService.Object, _conflogQuery.Object);
    }

    [Fact]
    public async Task FetchAndSave_WhenChanged_Returns_Ok_WithItem()
    {
        var entity = new VpnServerConflog { Id = 1, VpnServerId = 5, RequestUrl = "https://x", PayloadJson = "{}" };
        _conflogService.Setup(s => s.FetchAndSaveIfChangedAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new FetchAndSaveConflogRequest { BaseUrl = "https://x", VpnServerId = 5 };
        var result = await _controller.FetchAndSave(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<VpnServerConflogResponse?>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data!.Item);
        Assert.Equal(1, response.Data.Item.Id);
        Assert.Equal(5, response.Data.Item.VpnServerId);
        _conflogService.Verify(s => s.FetchAndSaveIfChangedAsync("https://x", 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchAndSave_WhenUnchanged_Returns_Ok_WithNullItem()
    {
        _conflogService.Setup(s => s.FetchAndSaveIfChangedAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServerConflog?)null);

        var request = new FetchAndSaveConflogRequest { BaseUrl = "https://y" };
        var result = await _controller.FetchAndSave(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<VpnServerConflogResponse?>>(ok.Value);
        Assert.True(response.Success);
        Assert.Null(response.Data);
    }

    [Fact]
    public async Task FetchAndSaveByServer_Returns_Ok_WithItem()
    {
        var entity = new VpnServerConflog { Id = 2, VpnServerId = 10, RequestUrl = "https://srv", PayloadJson = "{}" };
        _conflogService.Setup(s => s.FetchAndSaveIfChangedByServerIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _controller.FetchAndSaveByServer(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<VpnServerConflogResponse?>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data?.Item);
        Assert.Equal(10, response.Data.Item.VpnServerId);
        _conflogService.Verify(s => s.FetchAndSaveIfChangedByServerIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenFound_Returns_Ok()
    {
        var entity = new VpnServerConflog { Id = 7, RequestUrl = "https://x", PayloadJson = "{}", CreateDate = DateTimeOffset.UtcNow };
        _conflogQuery.Setup(q => q.GetById(7, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _controller.GetById(7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<VpnServerConflogResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data?.Item);
        Assert.Equal(7, response.Data.Item.Id);
        _conflogQuery.Verify(q => q.GetById(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns_NotFound()
    {
        _conflogQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServerConflog?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHistoryByServer_Returns_Ok_WithPagedItems()
    {
        var paged = new DataGateMonitor.SharedModels.Responses.PagedResponse<VpnServerConflog>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            Items = new List<VpnServerConflog>()
        };
        _conflogQuery.Setup(q => q.GetPageByVpnServerId(5, 1, 20, null, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await _controller.GetHistoryByServer(5, new GetConflogHistoryByServerRequest { Page = 1, PageSize = 20 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<VpnServerConflogPageResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data.Page);
        Assert.Equal(20, response.Data.PageSize);
        _conflogQuery.Verify(q => q.GetPageByVpnServerId(5, 1, 20, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryByServer_Passes_RequestUrl_To_Query()
    {
        var paged = new DataGateMonitor.SharedModels.Responses.PagedResponse<VpnServerConflog>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            Items = new List<VpnServerConflog>()
        };
        _conflogQuery
            .Setup(q => q.GetPageByVpnServerId(5, 1, 20, "/api/info", It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var request = new GetConflogHistoryByServerRequest { Page = 1, PageSize = 20, RequestUrl = "/api/info" };
        await _controller.GetHistoryByServer(5, request, CancellationToken.None);

        _conflogQuery.Verify(q => q.GetPageByVpnServerId(5, 1, 20, "/api/info", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPage_Returns_Ok_WithPagedItems()
    {
        var paged = new DataGateMonitor.SharedModels.Responses.PagedResponse<VpnServerConflog>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            Items = new List<VpnServerConflog>()
        };
        _conflogQuery.Setup(q => q.GetPage(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await _controller.GetPage(1, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<VpnServerConflogPageResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        _conflogQuery.Verify(q => q.GetPage(1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
