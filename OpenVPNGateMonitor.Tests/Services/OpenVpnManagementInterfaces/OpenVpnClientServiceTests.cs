using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.OpenVpnTelnet;

namespace OpenVPNGateMonitor.Tests.Services.OpenVpnManagementInterfaces;

public class OpenVpnClientServiceTests
{
    private readonly Mock<ILogger<IOpenVpnClientService>> _loggerMock;
    private readonly Mock<IGeoLiteQueryService> _geoLiteQueryServiceMock;
    private readonly Mock<ICommandQueue> _commandQueueMock;
    private readonly OpenVpnClientService _service;

    public OpenVpnClientServiceTests()
    {
        _loggerMock = new Mock<ILogger<IOpenVpnClientService>>();
        _geoLiteQueryServiceMock = new Mock<IGeoLiteQueryService>();
        _commandQueueMock = new Mock<ICommandQueue>();
        _service = new OpenVpnClientService(_loggerMock.Object, _geoLiteQueryServiceMock.Object);
    }

    [Fact]
    public async Task GetClientsAsync_WithValidResponse_ReturnsCorrectClients()
    {
        // Arrange
        var statusResponse = @"TITLE	OpenVPN 2.4.7 x86_64-pc-linux-gnu
HEADER	CLIENT_LIST	Common Name	Real Address	Virtual Address	Bytes Received	Bytes Sent	Connected Since	Connected Since (time_t)	Username
CLIENT_LIST	client1	192.168.1.100:1194	10.8.0.2	1000	2000	2023-06-15 10:00:00	1686823200	UNDEF
CLIENT_LIST	client2	192.168.1.101:1194	10.8.0.3	3000	4000	2023-06-15 10:15:00	1686824100	user2
END";

        var geoInfo = new OpenVpnGeoInfo
        {
            Country = "United States",
            Region = "California",
            City = "San Francisco",
            Latitude = 37.7749,
            Longitude = -122.4194
        };

        _commandQueueMock
            .Setup(x => x.SendCommandAsync("status 3", It.IsAny<CancellationToken>(), 5000))
            .ReturnsAsync(statusResponse);

        _geoLiteQueryServiceMock
            .Setup(x => x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(geoInfo);

        // Act
        var result = await _service.GetClientsAsync(_commandQueueMock.Object, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);

        var client1 = result[0];
        client1.CommonName.Should().Be("client1");
        client1.RemoteIp.Should().Be("192.168.1.100");
        client1.LocalIp.Should().Be("10.8.0.2");
        client1.BytesReceived.Should().Be(1000);
        client1.BytesSent.Should().Be(2000);
        client1.ConnectedSince.Should().Be(DateTime.Parse("2023-06-15 10:00:00", CultureInfo.InvariantCulture));
        client1.Username.Should().Be("client1");
        client1.Country.Should().Be("United States");
        client1.Region.Should().Be("California");
        client1.City.Should().Be("San Francisco");
        client1.Latitude.Should().Be(37.7749);
        client1.Longitude.Should().Be(-122.4194);

        var client2 = result[1];
        client2.CommonName.Should().Be("client2");
        client2.RemoteIp.Should().Be("192.168.1.101");
        client2.LocalIp.Should().Be("10.8.0.3");
        client2.BytesReceived.Should().Be(3000);
        client2.BytesSent.Should().Be(4000);
        client2.ConnectedSince.Should().Be(DateTime.Parse("2023-06-15 10:15:00", CultureInfo.InvariantCulture));
        client2.Username.Should().Be("user2");

        _commandQueueMock.Verify(x =>
            x.SendCommandAsync("status 3", It.IsAny<CancellationToken>(), 5000), Times.Once);
    }

    [Fact]
    public async Task GetClientsAsync_WithEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        _commandQueueMock
            .Setup(x => x.SendCommandAsync("status 3", It.IsAny<CancellationToken>(), 5000))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _service.GetClientsAsync(_commandQueueMock.Object, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _geoLiteQueryServiceMock.Verify(x => 
            x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetClientsAsync_WithInvalidClientData_SkipsInvalidClients()
    {
        // Arrange
        var statusResponse = @"TITLE	OpenVPN 2.4.7
HEADER	CLIENT_LIST	Common Name	Real Address	Virtual Address	Bytes Received	Bytes Sent	Connected Since	Connected Since (time_t)	Username
CLIENT_LIST	client1	invalid:ip	10.8.0.2	invalid	invalid	invalid-date	timestamp	UNDEF
CLIENT_LIST	client2	192.168.1.101:1194	10.8.0.3	3000	4000	2023-06-15 10:15:00	1686824100	user2
END";

        _commandQueueMock
            .Setup(x => x.SendCommandAsync("status 3", It.IsAny<CancellationToken>(), 5000))
            .ReturnsAsync(statusResponse);

        _geoLiteQueryServiceMock
            .Setup(x => x.GetGeoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnGeoInfo());

        // Act
        var result = await _service.GetClientsAsync(_commandQueueMock.Object, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var client = result[0];
        client.CommonName.Should().Be("client2");
        client.RemoteIp.Should().Be("192.168.1.101");
        client.LocalIp.Should().Be("10.8.0.3");
        client.BytesReceived.Should().Be(3000);
        client.BytesSent.Should().Be(4000);
        client.ConnectedSince.Should().Be(DateTime.Parse("2023-06-15 10:15:00", CultureInfo.InvariantCulture));
        client.Username.Should().Be("user2");

        _commandQueueMock.Verify(x => 
            x.SendCommandAsync("status 3", It.IsAny<CancellationToken>(), 5000), Times.Once);
    }

    [Fact]
    public async Task GetClientsAsync_WhenCommandFails_ThrowsException()
    {
        // Arrange
        _commandQueueMock
            .Setup(x => x.SendCommandAsync("status 3", It.IsAny<CancellationToken>(), 5000))
            .ThrowsAsync(new Exception("Command failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetClientsAsync(_commandQueueMock.Object, CancellationToken.None));
    }
}