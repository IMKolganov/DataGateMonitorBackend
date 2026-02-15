using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Services.QuotaPlans;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class QuotaPlanAllowedServerControllerTests
{
    private readonly Mock<IQuotaPlanAllowedServerService> _service = new(MockBehavior.Strict);
    private readonly QuotaPlanAllowedServerController _controller;

    public QuotaPlanAllowedServerControllerTests()
    {
        _controller = new QuotaPlanAllowedServerController(_service.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResponse()
    {
        var expected = new GetAllQuotaPlanAllowedServersResponse
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            Items = [new QuotaPlanAllowedServerDto { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }]
        };

        _service
            .Setup(s => s.GetPageAsync(It.IsAny<GetAllQuotaPlanAllowedServersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetAllQuotaPlanAllowedServersRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetAllQuotaPlanAllowedServersResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data!.Page);
        Assert.Equal(1, response.Data.TotalCount);
        Assert.Single(response.Data.Items);
        Assert.Equal(10, response.Data.Items[0].QuotaPlanId);
        Assert.Equal(5, response.Data.Items[0].VpnServerId);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var expected = new QuotaPlanAllowedServerResponse
        {
            QuotaPlanAllowedServer = new QuotaPlanAllowedServerDto { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<QuotaPlanAllowedServerResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data!.QuotaPlanAllowedServer.Id);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _service
            .Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuotaPlanAllowedServerResponse?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<QuotaPlanAllowedServerResponse>>(notFound.Value);
        Assert.False(response.Success);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetByQuotaPlanId_ReturnsOk_WithItems()
    {
        var items = new List<QuotaPlanAllowedServerDto>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.GetListByQuotaPlanIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _controller.GetByQuotaPlanId(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetQuotaPlanAllowedServersByQuotaPlanIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data!.Items);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetByVpnServerId_ReturnsOk_WithItems()
    {
        var items = new List<QuotaPlanAllowedServerDto>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.GetListByVpnServerIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _controller.GetByVpnServerId(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetQuotaPlanAllowedServersByVpnServerIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.Items);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Create_ReturnsOk_WithCreated()
    {
        var request = new CreateOrUpdateQuotaPlanAllowedServerRequest
        {
            QuotaPlanId = 10,
            VpnServerId = 5
        };
        var expected = new QuotaPlanAllowedServerResponse
        {
            QuotaPlanAllowedServer = new QuotaPlanAllowedServerDto { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.CreateAsync(It.IsAny<CreateOrUpdateQuotaPlanAllowedServerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<QuotaPlanAllowedServerResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Data!.QuotaPlanAllowedServer.Id);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var request = new CreateOrUpdateQuotaPlanAllowedServerRequest
        {
            Id = 1,
            QuotaPlanId = 10,
            VpnServerId = 5
        };

        _service
            .Setup(s => s.UpdateAsync(It.IsAny<CreateOrUpdateQuotaPlanAllowedServerRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        _service
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _service.VerifyAll();
    }
}
