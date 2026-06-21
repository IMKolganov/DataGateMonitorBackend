using Microsoft.Extensions.DependencyInjection;
using Moq;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Helpers;
using DataGateMonitor.Services.Others;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class GeoLiteLoadConfigsTests
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
    public async Task GetStringParamFromSettings_Returns_Value_When_Type_Is_String()
    {
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
                .ReturnsAsync("string");
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path", It.IsAny<CancellationToken>()))
                .ReturnsAsync("/data/GeoLite2-City.mmdb");
        });

        var value = await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Db_Path", sp, CancellationToken.None);

        Assert.Equal("/data/GeoLite2-City.mmdb", value);
    }

    [Fact]
    public async Task GetStringParamFromSettings_Throws_When_Type_Missing()
    {
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Db_Path", sp, CancellationToken.None));
    }

    [Fact]
    public async Task GetStringParamFromSettings_Throws_When_Type_Is_Not_String()
    {
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
                .ReturnsAsync("int");
        });

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Db_Path", sp, CancellationToken.None));
        Assert.Contains("not string", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStringParamFromSettings_Throws_When_Value_Missing()
    {
        var sp = BuildProvider(m =>
        {
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
                .ReturnsAsync("string");
            m.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Db_Path", sp, CancellationToken.None));
    }
}
