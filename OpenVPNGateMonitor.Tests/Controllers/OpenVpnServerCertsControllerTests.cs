using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Mapping.OpenVpnServerCerts.Mappings;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnServerCertsControllerTests
{
    private readonly Mock<ICertApiClient> _certApiClient = new();
    private readonly Mock<ILogger<OpenVpnServerCertsController>> _logger = new();

    public OpenVpnServerCertsControllerTests()
    {
        // Ensure Mapster mapping is registered for tests
        new VpnServerCertificateMapping().Register(TypeAdapterConfig.GlobalSettings);
    }

    private OpenVpnServerCertsController CreateController() =>
        new(_certApiClient.Object, _logger.Object);

    [Fact]
    public async Task GetAllCertificates_ReturnsOk_WithMappedResponse()
    {
        // Arrange
        var vpnServerId = 10;
        var request = new GetAllCertificatesRequest { VpnServerId = vpnServerId };

        var certificates = new List<ServerCertificate>
        {
            new()
            {
                CommonName = "clientA",
                Status = CertificateStatus.Active,
                SerialNumber = "ABC123",
                IsRevoked = false,
                Message = "ok",
                CertificatePath = "/certs/a.crt",
                KeyPath = "/keys/a.key",
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(30)
            },
            new()
            {
                CommonName = "clientB",
                Status = CertificateStatus.Revoked,
                SerialNumber = "XYZ789",
                IsRevoked = true,
                Message = "revoked",
                CertificatePath = "/certs/b.crt",
                KeyPath = "/keys/b.key",
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(60),
                RevokeDate = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _certApiClient
            .Setup(c => c.GetAllCertificatesAsync(vpnServerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificates);

        var controller = CreateController();

        // Act
        var actionResult = await controller.GetAllCertificates(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<GetAllCertificatesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data!.MonitorServerCertificates.Count);
        Assert.All(response.Data!.MonitorServerCertificates, c => Assert.Equal(vpnServerId, c.VpnServerId));
        Assert.Contains(response.Data!.MonitorServerCertificates, c => c.CommonName == "clientA");
        Assert.Contains(response.Data!.MonitorServerCertificates, c => c.CommonName == "clientB");
    }

    [Fact]
    public async Task GetAllCertificates_OnException_ReturnsBadRequest_AndLogsError()
    {
        // Arrange
        var vpnServerId = 11;
        var request = new GetAllCertificatesRequest { VpnServerId = vpnServerId };
        var ex = new Exception("boom");

        _certApiClient
            .Setup(c => c.GetAllCertificatesAsync(vpnServerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var controller = CreateController();

        // Act
        var actionResult = await controller.GetAllCertificates(request, CancellationToken.None);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("boom", response.Message, StringComparison.OrdinalIgnoreCase);

        _logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get certificates")),
                It.Is<Exception>(e => e == ex),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
    }

    [Fact]
    public async Task BuildCertificate_ReturnsOk_WithMappedResponse()
    {
        // Arrange
        var request = new BuildCertificateRequest { VpnServerId = 22, CommonName = "clientZ" };
        var cert = new ServerCertificate
        {
            CommonName = request.CommonName,
            Status = CertificateStatus.Active,
            SerialNumber = "S123"
        };

        _certApiClient
            .Setup(c => c.BuildCertificateAsync(request.VpnServerId, request.CommonName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var controller = CreateController();

        // Act
        var actionResult = await controller.BuildCertificate(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<BuildCertificateResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        var dto = response.Data!.MonitorServerCertificate;
        Assert.Equal(request.VpnServerId, dto.VpnServerId);
        Assert.Equal(request.CommonName, dto.CommonName);
    }

    [Fact]
    public async Task BuildCertificate_OnException_ReturnsBadRequest_AndLogsError()
    {
        // Arrange
        var request = new BuildCertificateRequest { VpnServerId = 23, CommonName = "oops" };
        var ex = new Exception("failed to build");

        _certApiClient
            .Setup(c => c.BuildCertificateAsync(request.VpnServerId, request.CommonName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var controller = CreateController();

        // Act
        var actionResult = await controller.BuildCertificate(request, CancellationToken.None);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("failed", response.Message, StringComparison.OrdinalIgnoreCase);

        _logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to build certificate")),
                It.Is<Exception>(e => e == ex),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
    }

    [Fact]
    public async Task RevokeCertificate_ReturnsOk_WithMappedResponse()
    {
        // Arrange
        var request = new RevokeCertificateRequest { VpnServerId = 33, CommonName = "clientY" };
        var cert = new ServerCertificate
        {
            CommonName = request.CommonName,
            Status = CertificateStatus.Revoked,
            SerialNumber = "R999",
            IsRevoked = true,
            RevokeDate = DateTimeOffset.UtcNow
        };

        _certApiClient
            .Setup(c => c.RevokeCertificateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var controller = CreateController();

        // Act
        var actionResult = await controller.RevokeCertificate(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<RevokeCertificateResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        var dto = response.Data!.MonitorServerCertificate;
        Assert.Equal(request.VpnServerId, dto.VpnServerId);
        Assert.Equal(request.CommonName, dto.CommonName);
        Assert.True(dto.IsRevoked);
    }

    [Fact]
    public async Task RevokeCertificate_OnException_ReturnsBadRequest_AndLogsError()
    {
        // Arrange
        var request = new RevokeCertificateRequest { VpnServerId = 34, CommonName = "bad" };
        var ex = new Exception("revoke error");

        _certApiClient
            .Setup(c => c.RevokeCertificateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var controller = CreateController();

        // Act
        var actionResult = await controller.RevokeCertificate(request, CancellationToken.None);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<string>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("revoke", response.Message, StringComparison.OrdinalIgnoreCase);

        _logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to revoke certificate")),
                It.Is<Exception>(e => e == ex),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once);
    }
}
