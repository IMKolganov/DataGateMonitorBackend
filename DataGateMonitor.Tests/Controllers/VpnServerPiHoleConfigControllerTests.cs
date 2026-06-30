using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DataGateMonitor.Tests.Controllers;

public class VpnServerPiHoleConfigControllerTests
{
    [Fact]
    public async Task Get_ReturnsConfig()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        service.Setup(x => x.GetForAdminAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServerPiHoleConfigResponse
            {
                Config = new VpnServerPiHoleConfigDto
                {
                    VpnServerId = 2,
                    BaseUrl = "http://pi-hole:8080"
                }
            });

        var controller = new VpnServerPiHoleConfigController(service.Object);
        var result = await controller.Get(2, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<VpnServerPiHoleConfigResponse>>(ok.Value);
        Assert.Equal("http://pi-hole:8080", envelope.Data!.Config.BaseUrl);
    }

    [Fact]
    public async Task Upsert_ReturnsBadRequest_WhenVpnServerIdInvalid()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        var controller = new VpnServerPiHoleConfigController(service.Object);

        var result = await controller.Upsert(new UpsertVpnServerPiHoleConfigRequest
        {
            VpnServerId = 0,
            BaseUrl = "http://pi-hole:8080"
        }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<VpnServerPiHoleConfigResponse>>(bad.Value);
        Assert.False(envelope.Success);
    }

    [Fact]
    public async Task Upsert_ReturnsBadRequest_WhenBaseUrlMissing()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        var controller = new VpnServerPiHoleConfigController(service.Object);

        var result = await controller.Upsert(new UpsertVpnServerPiHoleConfigRequest
        {
            VpnServerId = 1,
            BaseUrl = "  "
        }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<VpnServerPiHoleConfigResponse>>(bad.Value);
        Assert.False(envelope.Success);
    }

    [Fact]
    public async Task ApplyRuntime_ReturnsBadRequest_WhenVpnServerIdInvalid()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        var controller = new VpnServerPiHoleConfigController(service.Object);

        var result = await controller.ApplyRuntime(0, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(((ApiResponse<object>)bad.Value!).Success);
    }

    [Fact]
    public async Task ApplyRuntime_ReturnsOk_WhenServiceSucceeds()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        service.Setup(x => x.ApplyRuntimeToMicroserviceAsync(4, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VpnServerPiHoleConfigController(service.Object);
        var result = await controller.ApplyRuntime(4, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(((ApiResponse<object>)ok.Value!).Success);
    }

    [Fact]
    public async Task GetDiagnostics_ReturnsOk_WhenServiceSucceeds()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        service.Setup(x => x.GetMicroserviceDiagnosticsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PiHoleDiagnosticsResponse { Health = "Ok", BaseUrl = "http://pi-hole:8080" });

        var controller = new VpnServerPiHoleConfigController(service.Object);
        var result = await controller.GetDiagnostics(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<PiHoleDiagnosticsResponse>>(ok.Value);
        Assert.Equal("Ok", envelope.Data!.Health);
    }

    [Fact]
    public async Task GetDiagnostics_ReturnsBadRequest_WhenServiceThrows()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        service.Setup(x => x.GetMicroserviceDiagnosticsAsync(5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API URL is not set for the server."));

        var controller = new VpnServerPiHoleConfigController(service.Object);
        var result = await controller.GetDiagnostics(5, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<PiHoleDiagnosticsResponse>>(bad.Value);
        Assert.Contains("API URL", envelope.Message);
    }

    [Fact]
    public async Task GetRuntime_ReturnsNotFound_WhenDisabled()
    {
        var service = new Mock<IVpnServerPiHoleConfigService>();
        service.Setup(x => x.GetRuntimeForMicroserviceAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServerPiHoleRuntimeConfigResponse?)null);

        var controller = new VpnServerPiHoleConfigController(service.Object);
        var result = await controller.GetRuntime(3, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<VpnServerPiHoleRuntimeConfigResponse>>(notFound.Value);
        Assert.False(envelope.Success);
    }
}
