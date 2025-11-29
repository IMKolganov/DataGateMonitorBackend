using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnFilesControllerTests
{
    private readonly Mock<IOvpnFileApiService> _service = new();
    private readonly Mock<ILogger<OpenVpnFilesController>> _logger = new();
    private readonly OpenVpnFilesController _controller;

    public OpenVpnFilesControllerTests()
    {
        _controller = new OpenVpnFilesController(_service.Object, _logger.Object);
    }

    [Fact]
    public async Task GetByToken_Returns_BadRequest_When_Token_Empty()
    {
        var result = await _controller.GetByToken(new ByTokenRequest { Token = "  " }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllByVpnServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByVpnServerIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVPNGateMonitor.Models.IssuedOvpnFile>());

        var result = await _controller.GetAllByVpnServerId(new ByVpnServerIdRequest { VpnServerId = 5 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task AddFile_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.AddOvpnFileAsync(It.IsAny<AddFileRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _controller.AddFile(new AddFileRequest { CommonName = "cn", VpnServerId = 1 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }
}
