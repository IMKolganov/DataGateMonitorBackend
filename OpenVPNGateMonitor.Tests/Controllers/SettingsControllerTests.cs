using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Settings.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Settings.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly Mock<ISettingsService> _settingsMock = new(MockBehavior.Strict);
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _controller = new SettingsController(_settingsMock.Object);
    }

    [Fact]
    public async Task Get_Returns_NotFound_When_Type_Missing()
    {
        // Arrange
        var req = new GetSettingRequest { Key = "MyKey" };
        _settingsMock
            .Setup(s => s.GetValueAsync<string>("MyKey_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.Get(req, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(notFound.Value);

        Assert.False(response.Success);
        Assert.Equal("Setting 'MyKey' not found.", response.Message);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Get_Returns_Ok_When_Type_And_Value_Exist()
    {
        // Arrange
        var req = new GetSettingRequest { Key = "Flag" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("Flag_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bool");

        _settingsMock
            .Setup(s => s.GetValueAsync<bool>("Flag", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Get(req, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal("Flag", response.Data.Key);
        Assert.Equal(true, response.Data.Value);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Set_Returns_BadRequest_When_Invalid_Value_For_Type()
    {
        // Arrange
        var req = new SetSettingRequest { Key = "MyKey", Type = "int", Value = "abc" };

        // Act
        var result = await _controller.Set(req, CancellationToken.None);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);

        Assert.False(response.Success);
        Assert.Equal("Invalid value 'abc' for type 'int'.", response.Message);

        // Strict mock ensures no calls were made to ISettingsService
        _settingsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Set_Sets_Values_And_Returns_Ok()
    {
        // Arrange
        var req = new SetSettingRequest { Key = "Flag", Type = "bool", Value = "true" };

        _settingsMock
            .Setup(s => s.SetValueAsync<object>(
                "Flag",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _settingsMock
            .Setup(s => s.SetValueAsync<string>(
                "Flag_Type",
                "bool",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Set(req, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal("Flag", response.Data.Key);
        Assert.Equal(true, response.Data.Value);

        _settingsMock.VerifyAll();
    }

}
