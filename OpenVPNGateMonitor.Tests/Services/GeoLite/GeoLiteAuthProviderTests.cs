using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenVPNGateMonitor.Services.GeoLite;
using OpenVPNGateMonitor.Services.Others;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.GeoLite;

public class GeoLiteAuthProviderTests
{
    private static IServiceProvider BuildProvider(Action<Mock<ISettingsService>> setup)
    {
        var services = new ServiceCollection();
        var settingsMock = new Mock<ISettingsService>(MockBehavior.Strict);
        setup(settingsMock);
        services.AddScoped(_ => settingsMock.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetDownloadUrlAsync_Returns_Url_From_Settings()
    {
        // Arrange
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Download_Url_Type", It.IsAny<CancellationToken>()))
                .ReturnsAsync("string");
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Download_Url", It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://example.com/file");
        });

        var sut = new GeoLiteAuthProvider(sp);

        // Act
        var url = await sut.GetDownloadUrlAsync(CancellationToken.None);

        // Assert
        Assert.Equal("https://example.com/file", url);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_Throws_When_Type_Is_Not_String()
    {
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Download_Url_Type", It.IsAny<CancellationToken>()))
                .ReturnsAsync("int");
        });

        var sut = new GeoLiteAuthProvider(sp);
        await Assert.ThrowsAsync<Exception>(() => sut.GetDownloadUrlAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetBasicAuthHeaderAsync_Composes_Base64_AccountId_And_LicenseKey()
    {
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Account_ID_Type", It.IsAny<CancellationToken>())).ReturnsAsync("string");
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Account_ID", It.IsAny<CancellationToken>())).ReturnsAsync("acc");
            m.Setup(s => s.GetValueAsync<string>("GeoIp_License_Key_Type", It.IsAny<CancellationToken>())).ReturnsAsync("string");
            m.Setup(s => s.GetValueAsync<string>("GeoIp_License_Key", It.IsAny<CancellationToken>())).ReturnsAsync("key");
        });

        var sut = new GeoLiteAuthProvider(sp);
        var header = await sut.GetBasicAuthHeaderAsync(CancellationToken.None);

        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("acc:key")), header);
    }

    [Fact]
    public async Task GetBasicAuthHeaderAsync_Throws_When_Any_Is_Missing()
    {
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Account_ID_Type", It.IsAny<CancellationToken>())).ReturnsAsync("string");
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Account_ID", It.IsAny<CancellationToken>())).ReturnsAsync((string)null);
        });

        var sut = new GeoLiteAuthProvider(sp);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetBasicAuthHeaderAsync(CancellationToken.None));
    }
}
