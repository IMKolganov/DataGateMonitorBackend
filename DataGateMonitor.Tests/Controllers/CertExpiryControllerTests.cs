using System.Security.Claims;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DataGateMonitor.Tests.Controllers;

public class CertExpiryControllerTests
{
    [Fact]
    public async Task RunCheck_ReturnsSuccessResponse()
    {
        var runId = Guid.NewGuid();
        var runner = new Mock<ICertExpiryScheduledCheckRunner>();
        runner.Setup(r => r.RunCheckAsync(It.IsAny<RunCertExpiryCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertExpiryCheckRunResponse
            {
                RunId = runId,
                Status = CertExpiryRunStatus.Completed,
                ScopeLabel = "All eligible servers",
                Summary = new()
            });

        var store = new CertExpiryRunHistoryStore();
        var access = new Mock<IVpnServerAccessQueryService>();

        var controller = CreateController(runner.Object, store, access.Object);

        var result = await controller.RunCheck(new RunCertExpiryCheckRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<CertExpiryCheckRunResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(runId, payload.Data!.RunId);
    }

    [Fact]
    public void GetRuns_ReturnsStoredSummaries()
    {
        var runId = Guid.NewGuid();
        var store = new CertExpiryRunHistoryStore();
        store.Save(new CertExpiryCheckRunResponse
        {
            RunId = runId,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = CertExpiryRunStatus.Completed,
            ScopeLabel = "Server #10",
            VpnServerId = 10,
            Summary = new() { ProfilesChecked = 3 }
        });

        var controller = CreateController(Mock.Of<ICertExpiryScheduledCheckRunner>(), store, Mock.Of<IVpnServerAccessQueryService>());

        var result = controller.GetRuns(limit: 10, vpnServerId: null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<GetCertExpiryRunsResponse>>(ok.Value);
        Assert.Single(payload.Data!.Runs);
        Assert.Equal(runId, payload.Data.Runs[0].RunId);
    }

    [Fact]
    public void GetRun_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(
            Mock.Of<ICertExpiryScheduledCheckRunner>(),
            new CertExpiryRunHistoryStore(),
            Mock.Of<IVpnServerAccessQueryService>());

        var result = controller.GetRun(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static CertExpiryController CreateController(
        ICertExpiryScheduledCheckRunner runner,
        ICertExpiryRunHistoryStore store,
        IVpnServerAccessQueryService access)
    {
        var controller = new CertExpiryController(runner, store, access);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Admin")],
                    "mock"))
            }
        };
        return controller;
    }
}
