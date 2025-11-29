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

    // ---------- GET tests ----------

    [Fact]
    public async Task Get_Returns_NotFound_When_Type_Missing()
    {
        var req = new GetSettingRequest { Key = "MyKey" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("MyKey_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _controller.Get(req, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(notFound.Value);

        Assert.False(response.Success);
        Assert.Equal("Setting 'MyKey' not found.", response.Message);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Get_Returns_NotFound_When_Type_Unknown()
    {
        var req = new GetSettingRequest { Key = "WeirdKey" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("WeirdKey_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("unknown-type");

        var result = await _controller.Get(req, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(notFound.Value);

        Assert.False(response.Success);
        Assert.Equal("Setting 'WeirdKey' not found.", response.Message);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Get_Returns_NotFound_When_Value_IsNull_For_Known_Type()
    {
        var req = new GetSettingRequest { Key = "EmptyString" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("EmptyString_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("string");

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("EmptyString", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _controller.Get(req, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(notFound.Value);

        Assert.False(response.Success);
        Assert.Equal("Setting 'EmptyString' not found.", response.Message);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Get_Returns_Ok_When_Bool_Type_And_Value_Exist()
    {
        var req = new GetSettingRequest { Key = "Flag" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("Flag_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bool");

        _settingsMock
            .Setup(s => s.GetValueAsync<bool>("Flag", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Get(req, CancellationToken.None);

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
    public async Task Get_Returns_Ok_When_Int_Type()
    {
        var req = new GetSettingRequest { Key = "IntKey" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("IntKey_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("int");

        _settingsMock
            .Setup(s => s.GetValueAsync<int>("IntKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await _controller.Get(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("IntKey", response.Data!.Key);
        Assert.Equal(42, response.Data.Value);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Get_Returns_Ok_When_Double_Type()
    {
        var req = new GetSettingRequest { Key = "DoubleKey" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("DoubleKey_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("double");

        _settingsMock
            .Setup(s => s.GetValueAsync<double>("DoubleKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3.14);

        var result = await _controller.Get(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("DoubleKey", response.Data!.Key);
        Assert.Equal(3.14, (double)response.Data.Value!);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Get_Returns_Ok_When_DateTimeOffset_Type()
    {
        var req = new GetSettingRequest { Key = "DateKey" };
        var expected = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("DateKey_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("datetime");

        _settingsMock
            .Setup(s => s.GetValueAsync<DateTimeOffset>("DateKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Get(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("DateKey", response.Data!.Key);
        Assert.Equal(expected, (DateTimeOffset)response.Data.Value!);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Get_Returns_Ok_When_String_Type()
    {
        var req = new GetSettingRequest { Key = "StrKey" };

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("StrKey_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("string");

        _settingsMock
            .Setup(s => s.GetValueAsync<string>("StrKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync("hello");

        var result = await _controller.Get(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("StrKey", response.Data!.Key);
        Assert.Equal("hello", response.Data.Value);

        _settingsMock.VerifyAll();
    }

    // ---------- SET tests ----------

    [Fact]
    public async Task Set_Returns_BadRequest_When_Invalid_Value_For_Int()
    {
        var req = new SetSettingRequest { Key = "MyKey", Type = "int", Value = "abc" };

        var result = await _controller.Set(req, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);

        Assert.False(response.Success);
        Assert.Equal("Invalid value 'abc' for type 'int'.", response.Message);

        _settingsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Set_Returns_BadRequest_When_Unknown_Type()
    {
        var req = new SetSettingRequest { Key = "X", Type = "unknown", Value = "123" };

        var result = await _controller.Set(req, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);

        Assert.False(response.Success);
        Assert.Equal("Invalid value '123' for type 'unknown'.", response.Message);

        _settingsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Set_Sets_Bool_And_Returns_Ok()
    {
        var req = new SetSettingRequest { Key = "Flag", Type = "bool", Value = "true" };

        _settingsMock
            .Setup(s => s.SetValueAsync<object>(
                "Flag",
                It.Is<object>(o => o is bool && (bool)o),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _settingsMock
            .Setup(s => s.SetValueAsync<string>(
                "Flag_Type",
                "bool",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Set(req, CancellationToken.None);

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
    public async Task Set_Sets_Int_And_Returns_Ok()
    {
        var req = new SetSettingRequest { Key = "IntKey", Type = "int", Value = "42" };

        _settingsMock
            .Setup(s => s.SetValueAsync<object>(
                "IntKey",
                It.Is<object>(o => o is int && (int)o == 42),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _settingsMock
            .Setup(s => s.SetValueAsync<string>(
                "IntKey_Type",
                "int",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Set(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("IntKey", response.Data!.Key);
        Assert.Equal(42, response.Data.Value);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Set_Sets_Double_And_Returns_Ok()
    {
        var req = new SetSettingRequest { Key = "DoubleKey", Type = "double", Value = "3.14" };

        _settingsMock
            .Setup(s => s.SetValueAsync<object>(
                "DoubleKey",
                It.Is<object>(o => o is double && Math.Abs((double)o - 3.14) < 0.0001),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _settingsMock
            .Setup(s => s.SetValueAsync<string>(
                "DoubleKey_Type",
                "double",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Set(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("DoubleKey", response.Data!.Key);
        Assert.Equal(3.14, (double)response.Data.Value!);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Set_Sets_DateTimeOffset_And_Returns_Ok()
    {
        var req = new SetSettingRequest
        {
            Key = "DateKey",
            Type = "datetime",
            Value = "2024-01-02T03:04:05+00:00"
        };

        var expected = DateTimeOffset.Parse("2024-01-02T03:04:05+00:00");

        _settingsMock
            .Setup(s => s.SetValueAsync<object>(
                "DateKey",
                It.Is<object>(o => o is DateTimeOffset && (DateTimeOffset)o == expected),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _settingsMock
            .Setup(s => s.SetValueAsync<string>(
                "DateKey_Type",
                "datetime",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Set(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("DateKey", response.Data!.Key);
        Assert.Equal(expected, (DateTimeOffset)response.Data.Value!);

        _settingsMock.VerifyAll();
    }

    [Fact]
    public async Task Set_Sets_String_And_Returns_Ok()
    {
        var req = new SetSettingRequest { Key = "StrKey", Type = "string", Value = "hello" };

        _settingsMock
            .Setup(s => s.SetValueAsync<object>(
                "StrKey",
                It.Is<object>(o => o is string && (string)o == "hello"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _settingsMock
            .Setup(s => s.SetValueAsync<string>(
                "StrKey_Type",
                "string",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Set(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SettingResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("StrKey", response.Data!.Key);
        Assert.Equal("hello", response.Data.Value);

        _settingsMock.VerifyAll();
    }
}
