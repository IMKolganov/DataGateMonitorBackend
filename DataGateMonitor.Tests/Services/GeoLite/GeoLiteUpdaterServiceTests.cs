using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Interfaces;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class GeoLiteUpdaterServiceTests
{
    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

        public IList<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

        public void Enqueue(HttpResponseMessage response)
            => _responses.Enqueue(_ => response);

        public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> factory)
            => _responses.Enqueue(factory);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (_responses.Count == 0)
                throw new InvalidOperationException("No queued HTTP response.");

            return Task.FromResult(_responses.Dequeue()(request));
        }
    }

    private sealed class UpdaterTestContext : IAsyncDisposable
    {
        public required GeoLiteUpdaterService Sut { get; init; }
        public required QueueHandler Handler { get; init; }
        public required string DbPath { get; init; }
        public required string TempRoot { get; init; }
        public Mock<IGeoLiteProgressNotifier> Progress { get; init; } = null!;

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Directory.Exists(TempRoot))
                    Directory.Delete(TempRoot, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }

            await Task.CompletedTask;
        }
    }

    private static async Task<UpdaterTestContext> CreateContextAsync(
        Action<QueueHandler>? configureHttp = null,
        bool useRealStreamCopier = false,
        bool useRealDatabaseFactory = false)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "GeoLiteUpdater_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var dbPath = Path.Combine(tempRoot, "GeoLite2-City.mmdb");

        var handler = new QueueHandler();
        configureHttp?.Invoke(handler);
        var httpClient = new HttpClient(handler);

        GeoLiteDatabaseFactory dbFactory;
        if (useRealDatabaseFactory)
        {
            dbFactory = new GeoLiteDatabaseFactory(
                NullLogger<GeoLiteDatabaseFactory>.Instance,
                GeoLiteTestHarness.CreateServiceProvider(dbPath));
        }
        else
        {
            dbFactory = new Mock<GeoLiteDatabaseFactory>(MockBehavior.Loose, NullLogger<GeoLiteDatabaseFactory>.Instance, GeoLiteTestHarness.CreateServiceProvider(dbPath))
            {
                CallBase = true
            }.Object;
        }

        var config = new Mock<IGeoLiteConfigProvider>();
        config.Setup(c => c.CreateTimestamp()).Returns("20240101_000000");
        config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbPath);
        config.Setup(c => c.PreparePaths(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string db, string ts) =>
            {
                var dir = Path.GetDirectoryName(db)!;
                var extract = Path.Combine(dir, $"GeoLite2_{ts}");
                return (dir, extract, Path.Combine(extract, $"GeoLite2-City_{ts}.tar.gz"));
            });

        var auth = new Mock<IGeoLiteAuthProvider>();
        auth.Setup(a => a.GetDownloadUrlAsync(It.IsAny<CancellationToken>())).ReturnsAsync("https://example.com/geolite.tar.gz");
        auth.Setup(a => a.GetBasicAuthHeaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync("base64");

        var progress = new Mock<IGeoLiteProgressNotifier>();
        progress.Setup(p => p.ReportStepAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        progress.Setup(p => p.NotifyFinishedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        IStreamCopier streamCopier = useRealStreamCopier
            ? new StreamCopier(progress.Object)
            : new Mock<IStreamCopier>().Object;

        var sut = new GeoLiteUpdaterService(
            NullLogger<GeoLiteUpdaterService>.Instance,
            httpClient,
            dbFactory,
            config.Object,
            auth.Object,
            progress.Object,
            new HttpErrorMapper(NullLogger<HttpErrorMapper>.Instance),
            streamCopier);

        return new UpdaterTestContext
        {
            Sut = sut,
            Handler = handler,
            DbPath = dbPath,
            TempRoot = tempRoot,
            Progress = progress
        };
    }

    private static HttpResponseMessage CreateHeadersResponse(
        HttpStatusCode statusCode,
        DateTimeOffset? lastModified = null,
        long? contentLength = null,
        string? etag = null)
    {
        var response = new HttpResponseMessage(statusCode) { ReasonPhrase = "Test" };
        response.Content = new StringContent(string.Empty);
        if (lastModified is not null)
            response.Content.Headers.LastModified = lastModified;
        if (contentLength is not null)
            response.Content.Headers.ContentLength = contentLength;
        if (etag is not null)
            response.Headers.ETag = new EntityTagHeaderValue($"\"{etag}\"");
        return response;
    }

    [Fact]
    public async Task DownloadAndUpdateDatabaseAsync_Throws_On_Http_Error()
    {
        await using var ctx = await CreateContextAsync(h =>
            h.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized) { ReasonPhrase = "Test" }));

        await Assert.ThrowsAsync<Exception>(() => ctx.Sut.DownloadAndUpdateDatabaseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CheckNewVersionAsync_Throws_On_Http_Error()
    {
        await using var ctx = await CreateContextAsync(h =>
            h.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized) { ReasonPhrase = "Test" }));

        await Assert.ThrowsAsync<Exception>(() => ctx.Sut.CheckNewVersionAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CheckNewVersionAsync_Marks_Update_When_Local_File_Missing()
    {
        await using var ctx = await CreateContextAsync(h =>
            h.Enqueue(CreateHeadersResponse(HttpStatusCode.OK, DateTimeOffset.UtcNow, 12345, "etag-1")));

        var result = await ctx.Sut.CheckNewVersionAsync(CancellationToken.None);

        Assert.True(result.IsUpdateAvailable);
        Assert.Null(result.LocalLastWriteTimeUtc);
        Assert.Equal("https://example.com/geolite.tar.gz", result.CheckedUrl);
        Assert.Equal(HttpMethod.Head, ctx.Handler.Requests[0].Method);
    }

    [Fact]
    public async Task CheckNewVersionAsync_Falls_Back_To_Get_When_Head_Not_Allowed()
    {
        await using var ctx = await CreateContextAsync(h =>
        {
            h.Enqueue(new HttpResponseMessage(HttpStatusCode.MethodNotAllowed) { ReasonPhrase = "Test" });
            h.Enqueue(CreateHeadersResponse(HttpStatusCode.OK, DateTimeOffset.UtcNow, 12345, "etag-2"));
        });

        var result = await ctx.Sut.CheckNewVersionAsync(CancellationToken.None);

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(HttpMethod.Head, ctx.Handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Get, ctx.Handler.Requests[1].Method);
        Assert.Equal("etag-2", result.RemoteETag);
    }

    [Fact]
    public async Task CheckNewVersionAsync_No_Update_When_Remote_Not_Newer_Than_Local()
    {
        var localWrite = DateTime.UtcNow.AddDays(-1);
        await using var ctx = await CreateContextAsync(h =>
            h.Enqueue(CreateHeadersResponse(HttpStatusCode.OK, new DateTimeOffset(localWrite, TimeSpan.Zero), 10, "etag-3")));
        await File.WriteAllTextAsync(ctx.DbPath, "placeholder");
        File.SetLastWriteTimeUtc(ctx.DbPath, localWrite);

        var result = await ctx.Sut.CheckNewVersionAsync(CancellationToken.None);

        Assert.False(result.IsUpdateAvailable);
        Assert.NotNull(result.LocalLastWriteTimeUtc);
    }

    [Fact]
    public async Task CheckNewVersionAsync_Uses_ContentLength_When_LastModified_Missing()
    {
        await using var ctx = await CreateContextAsync(h =>
            h.Enqueue(CreateHeadersResponse(HttpStatusCode.OK, contentLength: 99999)));
        await File.WriteAllTextAsync(ctx.DbPath, "1234567890");
        File.SetLastWriteTimeUtc(ctx.DbPath, DateTime.UtcNow.AddDays(-30));

        var result = await ctx.Sut.CheckNewVersionAsync(CancellationToken.None);

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(99999, result.RemoteContentLength);
        Assert.Equal(10, result.LocalFileSize);
    }

    [Fact]
    public async Task DownloadAndUpdateDatabaseAsync_Replaces_Database_And_Reloads()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "GeoLiteUpdater_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var dbPath = Path.Combine(tempRoot, "GeoLite2-City.mmdb");
        var extractDir = Path.Combine(tempRoot, "GeoLite2_20240101_000000");
        Directory.CreateDirectory(extractDir);
        var tarGzPath = Path.Combine(extractDir, "GeoLite2-City_20240101_000000.tar.gz");
        await GeoLiteTestHarness.CreateTarGzWithMmdbAsync(tarGzPath, GeoLiteTestHarness.TestDatabaseSourcePath);

        var handler = new QueueHandler();
        var tarGzBytes = await File.ReadAllBytesAsync(tarGzPath);
        handler.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(tarGzBytes)
            };
            response.Content.Headers.ContentLength = tarGzBytes.Length;
            response.Content.Headers.LastModified = DateTimeOffset.UtcNow;
            return response;
        });

        var httpClient = new HttpClient(handler);
        var progress = new Mock<IGeoLiteProgressNotifier>();
        progress.Setup(p => p.ReportStepAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        progress.Setup(p => p.NotifyFinishedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var dbFactory = new GeoLiteDatabaseFactory(
            NullLogger<GeoLiteDatabaseFactory>.Instance,
            GeoLiteTestHarness.CreateServiceProvider(dbPath));

        var config = new Mock<IGeoLiteConfigProvider>();
        config.Setup(c => c.CreateTimestamp()).Returns("20240101_000000");
        config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbPath);
        config.Setup(c => c.PreparePaths(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string dir, string ts) =>
            {
                var extract = Path.Combine(tempRoot, $"GeoLite2_{ts}");
                return (tempRoot, extract, Path.Combine(extract, $"GeoLite2-City_{ts}.tar.gz"));
            });

        var auth = new Mock<IGeoLiteAuthProvider>();
        auth.Setup(a => a.GetDownloadUrlAsync(It.IsAny<CancellationToken>())).ReturnsAsync("https://example.com/geolite.tar.gz");
        auth.Setup(a => a.GetBasicAuthHeaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync("base64");

        var sut = new GeoLiteUpdaterService(
            NullLogger<GeoLiteUpdaterService>.Instance,
            httpClient,
            dbFactory,
            config.Object,
            auth.Object,
            progress.Object,
            new HttpErrorMapper(NullLogger<HttpErrorMapper>.Instance),
            new StreamCopier(progress.Object));

        try
        {
            var result = await sut.DownloadAndUpdateDatabaseAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(File.Exists(dbPath));
            Assert.True(new FileInfo(dbPath).Length > 1000);
            progress.Verify(p => p.NotifyFinishedAsync(It.IsAny<CancellationToken>()), Times.Once);

            var reader = await dbFactory.GetDatabaseAsync(CancellationToken.None);
            var city = reader.City("2.125.160.216");
            Assert.Equal("Boxford", city.City.Name);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
