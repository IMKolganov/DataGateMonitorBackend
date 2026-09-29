using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.AvailabilityCheck;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;
using DataGateMonitor.SharedModels.Responses;
using FluentAssertions;

namespace DataGateMonitor.Tests.Controllers;

public class AvailabilityCheckControllerTests
{
    private readonly Mock<IAvailabilityCheckRunner> _runner = new();
    private readonly Mock<IAvailabilityCheckStatusStore> _store = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly AvailabilityCheckController _controller;

    public AvailabilityCheckControllerTests()
    {
        _controller = new AvailabilityCheckController(_runner.Object, _store.Object, _settings.Object);
    }

    [Fact]
    public async Task GetStatus_ReturnsCached_WithoutRunningCheck()
    {
        var cached = new AvailabilityCheckStatusResponse { Enabled = true, ProbeUrl = "https://probe" };
        _store.Setup(s => s.Get()).Returns(cached);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<AvailabilityCheckStatusResponse>>(ok.Value);
        body.Success.Should().BeTrue();
        body.Data.Should().BeSameAs(cached);
        _runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GetStatus_WhenCacheMiss_RunsCheck()
    {
        var snapshot = new AvailabilityCheckStatusResponse { Enabled = true, ProbeUrl = "https://probe" };
        _store.Setup(s => s.Get()).Returns((AvailabilityCheckStatusResponse?)null);
        _runner.Setup(r => r.RunAsync(It.IsAny<CancellationToken>(), false)).ReturnsAsync(snapshot);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<AvailabilityCheckStatusResponse>>(ok.Value);
        body.Data.Should().BeSameAs(snapshot);
        _runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>(), false), Times.Once);
    }

    [Fact]
    public async Task UpdateSettings_InvalidProbeUrl_ReturnsBadRequest()
    {
        var result = await _controller.UpdateSettings(
            new UpdateAvailabilityCheckSettingsRequest { Enabled = true, ProbeUrl = "not-a-url" },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<AvailabilityCheckStatusResponse>>(bad.Value);
        body.Success.Should().BeFalse();
        body.Message.Should().Contain("ProbeUrl");
        _runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSettings_EmptyProbeUrl_UsesDefault_PersistsAndRunsWithForceMatchingEnabled()
    {
        var snapshot = new AvailabilityCheckStatusResponse { Enabled = true };
        _settings
            .Setup(s => s.SetValueAsync(AvailabilityCheckSettingsKeys.Enabled, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _settings
            .Setup(s => s.SetValueAsync(
                AvailabilityCheckSettingsKeys.ProbeUrl,
                AvailabilityCheckSettingsKeys.DefaultProbeUrl,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _runner.Setup(r => r.RunAsync(It.IsAny<CancellationToken>(), true)).ReturnsAsync(snapshot);

        var result = await _controller.UpdateSettings(
            new UpdateAvailabilityCheckSettingsRequest { Enabled = true, ProbeUrl = "  " },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<ApiResponse<AvailabilityCheckStatusResponse>>(ok.Value).Data.Should().BeSameAs(snapshot);

        _settings.Verify(
            s => s.SetValueAsync(AvailabilityCheckSettingsKeys.Enabled, true, It.IsAny<CancellationToken>()),
            Times.Once);
        _settings.Verify(
            s => s.SetValueAsync(
                AvailabilityCheckSettingsKeys.ProbeUrl,
                AvailabilityCheckSettingsKeys.DefaultProbeUrl,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>(), true), Times.Once);
    }

    [Fact]
    public async Task UpdateSettings_WhenDisabled_RunsWithForceFalse()
    {
        var snapshot = new AvailabilityCheckStatusResponse { Enabled = false };
        _settings
            .Setup(s => s.SetValueAsync(AvailabilityCheckSettingsKeys.Enabled, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _settings
            .Setup(s => s.SetValueAsync(
                AvailabilityCheckSettingsKeys.ProbeUrl,
                "https://status.rackot.ru/check.cgi",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _runner.Setup(r => r.RunAsync(It.IsAny<CancellationToken>(), false)).ReturnsAsync(snapshot);

        await _controller.UpdateSettings(
            new UpdateAvailabilityCheckSettingsRequest
            {
                Enabled = false,
                ProbeUrl = "https://status.rackot.ru/check.cgi",
            },
            CancellationToken.None);

        _runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>(), false), Times.Once);
    }

    [Fact]
    public async Task RunCheck_CallsRunnerWithForceTrue()
    {
        var snapshot = new AvailabilityCheckStatusResponse { Enabled = false };
        _runner.Setup(r => r.RunAsync(It.IsAny<CancellationToken>(), true)).ReturnsAsync(snapshot);

        var result = await _controller.RunCheck(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<ApiResponse<AvailabilityCheckStatusResponse>>(ok.Value).Data.Should().BeSameAs(snapshot);
        _runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>(), true), Times.Once);
    }
}
