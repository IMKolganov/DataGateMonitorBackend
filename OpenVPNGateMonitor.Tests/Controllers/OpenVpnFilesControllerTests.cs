using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnFilesControllerTests
{
    private readonly Mock<IOvpnFileApiService> _service = new();
    private readonly Mock<ILogger<OpenVpnFilesController>> _logger = new();
    private readonly Mock<IUserQuotaPlanQueryService> _userQuotaPlan = new();
    private readonly Mock<IQuotaPlanAllowedServerQueryService> _quotaAllowed = new();
    private readonly Mock<IVpnServerAccessQueryService> _vpnAccess = new();
    private readonly OpenVpnFilesController _controller;

    public OpenVpnFilesControllerTests()
    {
        _controller = new OpenVpnFilesController(_service.Object, _logger.Object, _userQuotaPlan.Object,
            _quotaAllowed.Object, _vpnAccess.Object);
        SetUserAsAdmin(_controller);
    }

    private static void SetUserAsAdmin(OpenVpnFilesController controller) =>
        SetUser(controller, new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin")],
            "mock")));

    private static void SetUser(OpenVpnFilesController controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
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
        _service.Setup(s => s.GetAllByVpnServerId(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVPNGateMonitor.Models.IssuedOvpnFile>());

        var result = await _controller.GetAllByVpnServerId(new ByVpnServerIdRequest { VpnServerId = 5 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task AddFile_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.AddOvpnFile(It.IsAny<AddFileRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _controller.AddFile(new AddFileRequest { CommonName = "cn", VpnServerId = 1 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetByToken_Returns_Ok()
    {
        _service.Setup(s => s.GetByToken("tkn", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new OpenVPNGateMonitor.Models.IssuedOvpnFile());

        var result = await _controller.GetByToken(new ByTokenRequest { Token = "tkn" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetByToken_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetByToken("tkn", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetByToken(new ByTokenRequest { Token = "tkn" }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllByVpnServerId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByVpnServerId(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetAllByVpnServerId(new ByVpnServerIdRequest { VpnServerId = 10 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllByExternalIdAndVpnServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerId(3, "ext", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<OpenVPNGateMonitor.Models.IssuedOvpnFile>());

        var result = await _controller.GetAllByExternalIdAndVpnServerId(
            new ByExternalIdAndVpnServerIdRequest { VpnServerId = 3, ExternalId = "ext" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAllByExternalIdAndVpnServerId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerId(3, "ext", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetAllByExternalIdAndVpnServerId(
            new ByExternalIdAndVpnServerIdRequest { VpnServerId = 3, ExternalId = "ext" }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByVpnServerIdWithToken(6, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<(OpenVPNGateMonitor.Models.IssuedOvpnFile, OpenVPNGateMonitor.Models.IssuedOvpnFileToken?)>());

        var result = await _controller.GetAllWithToken(new ByVpnServerIdRequest { VpnServerId = 6 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFilesWithTokensResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByServerId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByVpnServerIdWithToken(6, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetAllWithToken(new ByVpnServerIdRequest { VpnServerId = 6 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByExternalId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerIdWithToken(2, "e1", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<(OpenVPNGateMonitor.Models.IssuedOvpnFile, OpenVPNGateMonitor.Models.IssuedOvpnFileToken?)>());

        var result = await _controller.GetAllWithToken(new ByExternalIdAndVpnServerIdRequest { VpnServerId = 2, ExternalId = "e1" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFilesWithTokensResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByExternalId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerIdWithToken(2, "e1", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetAllWithToken(new ByExternalIdAndVpnServerIdRequest { VpnServerId = 2, ExternalId = "e1" }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetFiles_ByExternalId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalId("ext2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVPNGateMonitor.Models.IssuedOvpnFile>());

        var result = await _controller.GetFiles(new ByExternalIdRequest { ExternalId = "ext2" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetFiles_ByExternalId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByExternalId("ext2", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetFiles(new ByExternalIdRequest { ExternalId = "ext2" }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task AddFile_Returns_Ok()
    {
        _service.Setup(s => s.AddOvpnFile(It.IsAny<AddFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVPNGateMonitor.Models.IssuedOvpnFile());

        var result = await _controller.AddFile(new AddFileRequest { CommonName = "cn", VpnServerId = 1 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task AddFileWithToken_Returns_Ok()
    {
        _service.Setup(s => s.AddOvpnFileWithToken(It.IsAny<AddFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new OpenVPNGateMonitor.Models.IssuedOvpnFile(), new OpenVPNGateMonitor.Models.IssuedOvpnFileToken()));

        var result = await _controller.AddFileWithToken(new AddFileRequest { CommonName = "cn", VpnServerId = 1 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileWithTokenResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task AddFileWithToken_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.AddOvpnFileWithToken(It.IsAny<AddFileRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.AddFileWithToken(new AddFileRequest { CommonName = "cn", VpnServerId = 1 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task RevokeFile_Returns_Ok()
    {
        _service.Setup(s => s.RevokeOvpnFile(It.IsAny<RevokeFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVPNGateMonitor.Models.IssuedOvpnFile());

        var result = await _controller.RevokeFile(new RevokeFileRequest { CommonName = "cn", VpnServerId = 5 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task RevokeFile_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.RevokeOvpnFile(It.IsAny<RevokeFileRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.RevokeFile(new RevokeFileRequest { CommonName = "cn", VpnServerId = 5 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task DownloadFile_Returns_Ok()
    {
        _service.Setup(s => s.DownloadOvpnFile(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new DownloadFileResponse { Content = new byte[] { 1, 2, 3 }, FileSizeBytes = 3 });

        var result = await _controller.DownloadFile(new DownloadFileRequest { IssuedOvpnFileId = 1, VpnServerId = 1 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DownloadFileResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task DownloadFile_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.DownloadOvpnFile(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.DownloadFile(new DownloadFileRequest { IssuedOvpnFileId = 1, VpnServerId = 1 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllByVpnServerId_WhenVpnUserNoAccess_Returns_Forbidden()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "900")
            ],
            "mock")));
        _vpnAccess.Setup(a => a.UserHasAccessAsync(900, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.GetAllByVpnServerId(new ByVpnServerIdRequest { VpnServerId = 5 }, CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
        _service.Verify(s => s.GetAllByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByToken_WhenVpnUserNoAccess_Returns_Forbidden()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "901")
            ],
            "mock")));
        _service.Setup(s => s.GetByToken("tok", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new OpenVPNGateMonitor.Models.IssuedOvpnFile { VpnServerId = 12 });
        _vpnAccess.Setup(a => a.UserHasAccessAsync(901, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.GetByToken(new ByTokenRequest { Token = "tok" }, CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
    }
}
