using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class GeoLiteControllerTests
{
    private readonly Mock<IGeoLiteQueryService> queryServiceMock;
    private readonly Mock<IGeoLiteUpdaterService> updaterServiceMock;
    private readonly GeoLiteController controller;

    public GeoLiteControllerTests()
    {
        queryServiceMock = new Mock<IGeoLiteQueryService>();
        updaterServiceMock = new Mock<IGeoLiteUpdaterService>();

        controller = new GeoLiteController(queryServiceMock.Object, updaterServiceMock.Object);
    }

    // -------------------------
    // GetDatabasePath
    // -------------------------

    [Fact]
    public void GetDatabasePath_ReturnsOk_WithSuccessResponse()
    {
        // Arrange
        const string dbPath = "/var/lib/geolite/GeoLite2-City.mmdb";
        queryServiceMock.Setup(s => s.GetDatabasePath()).Returns(dbPath);

        // Act
        var result = controller.GetDatabasePath();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetDatabasePathResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(dbPath, response.Data.DatabasePath);

        queryServiceMock.Verify(s => s.GetDatabasePath(), Times.Once);
    }

    // -------------------------
    // GetGeoInfo
    // -------------------------

    [Fact]
    public async Task GetGeoInfo_WhenFound_ReturnsOk_WithGeoInfo()
    {
        // Arrange
        var request = new GetGeoInfoRequest { IpAddress = "8.8.8.8" };
        var geo = new OpenVpnGeoInfo
        {
            Country = "US",
            Region = "CA",
            City = "Mountain View",
            Latitude = 37.4056,
            Longitude = -122.0775
        };

        queryServiceMock
            .Setup(s => s.GetGeoInfoAsync(request.IpAddress!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(geo);

        var ct = CancellationToken.None;

        // Act
        var result = await controller.GetGeoInfo(request, ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetGeoInfoResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GeoInfo);
        Assert.Equal(geo.Country, response.Data.GeoInfo.Country);
        Assert.Equal(geo.City, response.Data.GeoInfo.City);

        queryServiceMock.Verify(s => s.GetGeoInfoAsync(request.IpAddress!, ct), Times.Once);
    }

    [Fact]
    public async Task GetGeoInfo_WhenNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new GetGeoInfoRequest { IpAddress = "203.0.113.10" };

        queryServiceMock
            .Setup(s => s.GetGeoInfoAsync(request.IpAddress!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenVpnGeoInfo?)null);

        var ct = CancellationToken.None;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await controller.GetGeoInfo(request, ct);
        });

        Assert.Equal("No geo information found for the provided IP address.", ex.Message);

        queryServiceMock.Verify(s => s.GetGeoInfoAsync(request.IpAddress!, ct), Times.Once);
    }

    // -------------------------
    // GetVersionDatabase
    // -------------------------

    [Fact]
    public async Task GetVersionDatabase_ReturnsOk_WithVersion()
    {
        // Arrange
        const string version = "2025-11-28";
        queryServiceMock
            .Setup(s => s.GetDatabaseVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        var ct = CancellationToken.None;

        // Act
        var result = await controller.GetVersionDatabase(ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetVersionDatabaseResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(version, response.Data.DatabaseVersion);

        queryServiceMock.Verify(s => s.GetDatabaseVersionAsync(ct), Times.Once);
    }

    // -------------------------
    // UpdateDatabase
    // -------------------------

    [Fact]
    public async Task UpdateDatabase_ReturnsOk_WithUpdateResult()
    {
        // Arrange
        var update = new GeoLiteUpdateResponse
        {
            Success = true,
            DownloadUrl = "https://example.com/geolite.tar.gz",
            TempFilePath = "/tmp/geolite.tar.gz",
            ExtractedPath = "/tmp/geolite",
            DatabasePath = "/var/lib/geolite/GeoLite2-City.mmdb"
        };

        updaterServiceMock
            .Setup(s => s.DownloadAndUpdateDatabaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);

        var ct = CancellationToken.None;

        // Act
        var result = await controller.UpdateDatabase(ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GeoLiteUpdateResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(update.Success, response.Data.Success);
        Assert.Equal(update.DatabasePath, response.Data.DatabasePath);

        updaterServiceMock.Verify(s => s.DownloadAndUpdateDatabaseAsync(ct), Times.Once);
    }

    // -------------------------
    // CheckNewVersion
    // -------------------------

    [Fact]
    public async Task CheckNewVersion_ReturnsOk_WithCheckResult()
    {
        // Arrange
        var check = new GeoLiteVersionCheckResponse
        {
            IsUpdateAvailable = true,
            RemoteLastModified = new DateTimeOffset(2025, 11, 28, 0, 0, 0, TimeSpan.Zero),
            RemoteETag = "etag-123",
            RemoteContentLength = 1234567,
            LocalLastWriteTimeUtc = new DateTimeOffset(2025, 11, 20, 0, 0, 0, TimeSpan.Zero),
            LocalFileSize = 1200000,
            CheckedUrl = "https://example.com/geolite/metadata.json"
        };

        updaterServiceMock
            .Setup(s => s.CheckNewVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(check);

        var ct = CancellationToken.None;

        // Act
        var result = await controller.CheckNewVersion(ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GeoLiteVersionCheckResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(check.IsUpdateAvailable, response.Data.IsUpdateAvailable);
        Assert.Equal(check.RemoteETag, response.Data.RemoteETag);
        Assert.Equal(check.CheckedUrl, response.Data.CheckedUrl);

        updaterServiceMock.Verify(s => s.CheckNewVersionAsync(ct), Times.Once);
    }
}
