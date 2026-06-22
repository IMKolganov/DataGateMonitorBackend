using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DataGateMonitor.Tests.Controllers;

public class VpnServerPiHoleConfigControllerTests
{
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
