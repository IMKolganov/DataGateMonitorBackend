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
}
