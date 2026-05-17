using FluentAssertions;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerOvpnFileConfig.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerOvpnFileConfig.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class VpnServerOvpnFileConfigControllerTests
{
    private readonly Mock<IVpnServerOvpnFileConfigService> _serviceMock;
    private readonly VpnServerOvpnFileConfigController _controller;

    public VpnServerOvpnFileConfigControllerTests()
    {
        _serviceMock = new Mock<IVpnServerOvpnFileConfigService>();
        _controller = new VpnServerOvpnFileConfigController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetOvpnFileConfig_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var vpnServerId = 1;
        var cancellationToken = CancellationToken.None;
        var expectedConfig = new VpnServerOvpnFileConfig
        {
            VpnServerId = vpnServerId,
            VpnServerIp = "1.2.3.4",
            VpnServerPort = 1194,
            ConfigTemplate = "template"
        };

        _serviceMock.Setup(s => s.GetVpnServerOvpnFileConfigByServerId(vpnServerId, cancellationToken))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _controller.GetOvpnFileConfig(new GetOvpnFileConfigRequest { VpnServerId = vpnServerId }, cancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileConfigResponse>>(okResult.Value);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.Equal(expectedConfig.VpnServerId, data.VpnServerId);
        Assert.Equal(expectedConfig.VpnServerIp, data.VpnServerIp);
        Assert.Equal(expectedConfig.VpnServerPort, data.VpnServerPort);
        Assert.Equal(expectedConfig.ConfigTemplate, data.ConfigTemplate);
    }

    [Fact]
    public async Task AddOrUpdateOvpnFileConfig_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var request = new AddOrUpdateOvpnFileConfigRequest
        {
            VpnServerId = 1,
            VpnServerIp = "5.6.7.8",
            VpnServerPort = 443,
            ConfigTemplate = "custom-template",
            AutoDetectServerSettings = true
        };

        var expectedConfig = request.Adapt<VpnServerOvpnFileConfig>();

        _serviceMock.Setup(s => s.AddOrUpdateVpnServerOvpnFileConfigByServerId(
                It.Is<VpnServerOvpnFileConfig>(c => c.VpnServerId == request.VpnServerId), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _controller.AddOrUpdateOvpnFileConfig(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileConfigResponse>>(okResult.Value);
        response.Should().NotBeNull();
        response.Data.Should().NotBeNull();

        var data = response.Data!;
        Assert.Equal(request.VpnServerId, data.VpnServerId);
        Assert.Equal(request.VpnServerIp, data.VpnServerIp);
        Assert.Equal(request.VpnServerPort, data.VpnServerPort);
        Assert.Equal(request.ConfigTemplate, data.ConfigTemplate);

    }
}