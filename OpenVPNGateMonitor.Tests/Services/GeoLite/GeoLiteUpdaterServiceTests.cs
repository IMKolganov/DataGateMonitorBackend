using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenVPNGateMonitor.Services.GeoLite;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;

namespace OpenVPNGateMonitor.Tests.Services.GeoLite;

public class GeoLiteUpdaterServiceTests
{
    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private static GeoLiteUpdaterService CreateSut(HttpStatusCode code)
    {
        var response = new HttpResponseMessage(code) { ReasonPhrase = "Test" };
        var handler = new StubHandler(response);
        var httpClient = new HttpClient(handler);

        var dbFactory = new Mock<GeoLiteDatabaseFactory>(MockBehavior.Loose, [null!, null!]) { CallBase = true };
        var config = new Mock<IGeoLiteConfigProvider>();
        var auth = new Mock<IGeoLiteAuthProvider>();
        var progress = new Mock<IGeoLiteProgressNotifier>();
        var mapper = new Mock<IHttpErrorMapper>();
        var copier = new Mock<IStreamCopier>();

        config.Setup(c => c.CreateTimestamp()).Returns("20240101_000000");
        config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"), "GeoLite2-City.mmdb"));
        config.Setup(c => c.PreparePaths(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string db, string ts) =>
            {
                var dir = System.IO.Path.GetDirectoryName(db)!;
                var extract = System.IO.Path.Combine(dir, $"GeoLite2_{ts}");
                return (dir, extract, System.IO.Path.Combine(extract, $"GeoLite2-City_{ts}.tar.gz"));
            });

        auth.Setup(a => a.GetDownloadUrlAsync(It.IsAny<CancellationToken>())).ReturnsAsync("https://example.com");
        auth.Setup(a => a.GetBasicAuthHeaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync("base64");

        mapper.Setup(m => m.Map(It.IsAny<HttpResponseMessage>())).Returns("mapped error");

        return new GeoLiteUpdaterService(
            new NullLogger<GeoLiteUpdaterService>(),
            httpClient,
            dbFactory.Object,
            config.Object,
            auth.Object,
            progress.Object,
            mapper.Object,
            copier.Object);
    }

    [Fact]
    public async Task DownloadAndUpdateDatabaseAsync_Throws_On_Http_Error()
    {
        var sut = CreateSut(HttpStatusCode.Unauthorized);
        await Assert.ThrowsAsync<Exception>(() => sut.DownloadAndUpdateDatabaseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CheckNewVersionAsync_Throws_On_Http_Error()
    {
        var sut = CreateSut(HttpStatusCode.Unauthorized);
        await Assert.ThrowsAsync<Exception>(() => sut.CheckNewVersionAsync(CancellationToken.None));
    }
}
