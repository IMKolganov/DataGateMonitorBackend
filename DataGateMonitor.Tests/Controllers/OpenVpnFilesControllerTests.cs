using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

/// <summary>
/// Legacy Android compatibility regression tests for profile/file APIs.
/// These expectations must stay compatible with old Android app installs.
/// </summary>
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
            .ReturnsAsync(new List<DataGateMonitor.Models.IssuedOvpnFile>());

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
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetByToken_Returns_Ok()
    {
        _service.Setup(s => s.GetByToken("tkn", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new DataGateMonitor.Models.IssuedOvpnFile());

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
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllByExternalIdAndVpnServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerId(3, "ext", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<DataGateMonitor.Models.IssuedOvpnFile>());

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
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByVpnServerIdWithToken(6, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<(DataGateMonitor.Models.IssuedOvpnFile, DataGateMonitor.Models.IssuedOvpnFileToken?)>());

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
        var response = Assert.IsType<ApiResponse<OvpnFilesWithTokensResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByExternalId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerIdWithToken(2, "e1", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<(DataGateMonitor.Models.IssuedOvpnFile, DataGateMonitor.Models.IssuedOvpnFileToken?)>());

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
        var response = Assert.IsType<ApiResponse<OvpnFilesWithTokensResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetFiles_ByExternalId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalId("ext2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataGateMonitor.Models.IssuedOvpnFile>());

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
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    [Trait("Compatibility", "LegacyAndroid")]
    public async Task GetFiles_WhenVpnUserAndNoQuotaPlan_DoesNotFilterOutFiles_ForLegacyAndroidCompatibility()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "777")
            ],
            "mock")));

        _userQuotaPlan.Setup(u => u.GetActiveByUserId(777, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataGateMonitor.Models.UserQuotaPlan?)null);
        _service.Setup(s => s.GetAllByExternalId("legacy-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new DataGateMonitor.Models.IssuedOvpnFile { VpnServerId = 10, CommonName = "legacy-cn" }
            ]);

        var result = await _controller.GetFiles(new ByExternalIdRequest { ExternalId = "legacy-user" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFilesResponse>>(ok.Value);
        Assert.True(response.Success);
        _service.Verify(s => s.GetAllByExternalId("legacy-user", It.IsAny<CancellationToken>()), Times.Once);
        _quotaAllowed.Verify(q => q.GetVpnServerIdsByQuotaPlanId(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddFile_Returns_Ok()
    {
        _service.Setup(s => s.AddOvpnFile(It.IsAny<AddFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataGateMonitor.Models.IssuedOvpnFile());

        var result = await _controller.AddFile(new AddFileRequest { CommonName = "cn", VpnServerId = 1 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task AddFileWithToken_Returns_Ok()
    {
        _service.Setup(s => s.AddOvpnFileWithToken(It.IsAny<AddFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new DataGateMonitor.Models.IssuedOvpnFile(), new DataGateMonitor.Models.IssuedOvpnFileToken()));

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
        var response = Assert.IsType<ApiResponse<OvpnFileWithTokenResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task RevokeFile_Returns_Ok()
    {
        _service.Setup(s => s.RevokeOvpnFile(It.IsAny<RevokeFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataGateMonitor.Models.IssuedOvpnFile());

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
        var response = Assert.IsType<ApiResponse<OvpnFileResponse>>(bad.Value);
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
        var response = Assert.IsType<ApiResponse<DownloadFileResponse>>(bad.Value);
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
            .ReturnsAsync(new DataGateMonitor.Models.IssuedOvpnFile { VpnServerId = 12 });
        _vpnAccess.Setup(a => a.UserHasAccessAsync(901, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.GetByToken(new ByTokenRequest { Token = "tok" }, CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
    }
}
