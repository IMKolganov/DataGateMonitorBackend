using DataGateMonitor.DataBase.Contexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataGateMonitor.Tests.Controllers;

public class MobileCrashIngestControllerIntegrationTests
{
    private static HttpRequestMessage CreateRequest(string body, string process = "com.imkolganov.datagate.dev")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/crash-ingest");
        req.Content = new StringContent(body);
        req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
        {
            CharSet = "utf-8"
        };
        req.Headers.TryAddWithoutValidation("X-Crash-Filename", "fatal_2026-05-01T00-00-00.000Z.txt");
        req.Headers.TryAddWithoutValidation("X-Crash-Process", process);
        return req;
    }

    [Fact]
    public async Task Ingest_WithValidPayload_ReturnsNoContentAndPersists()
    {
        await using var factory = new CrashIngestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var payload = """
                      process=com.imkolganov.datagate.dev
                      kind=fatal

                      java.lang.RuntimeException: boom
                      """;
        using var request = CreateRequest(payload);

        var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.MobileCrashReports.CountAsync());
    }

    [Fact]
    public async Task Ingest_WithEmptyBody_ReturnsBadRequest()
    {
        await using var factory = new CrashIngestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = CreateRequest(string.Empty);
        var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WhenPayloadTooLarge_ReturnsPayloadTooLarge()
    {
        await using var factory = new CrashIngestWebApplicationFactory(maxPayloadBytes: 32);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var payload = new string('A', 64);
        using var request = CreateRequest(payload);
        var response = await client.SendAsync(request);

        Assert.Equal((System.Net.HttpStatusCode)413, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WhenRateLimitExceeded_ReturnsTooManyRequests()
    {
        await using var factory = new CrashIngestWebApplicationFactory(rateLimitMaxRequests: 1, rateLimitWindowSeconds: 300);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var payload = """
                      process=com.imkolganov.datagate.dev

                      stacktrace
                      """;

        using var request1 = CreateRequest(payload, process: "com.imkolganov.datagate.dev");
        using var response1 = await client.SendAsync(request1);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response1.StatusCode);

        using var request2 = CreateRequest(payload, process: "com.imkolganov.datagate.dev");
        using var response2 = await client.SendAsync(request2);
        Assert.Equal((System.Net.HttpStatusCode)429, response2.StatusCode);
    }
}

file sealed class CrashIngestWebApplicationFactory(
    int maxPayloadBytes = 1024 * 1024,
    int rateLimitMaxRequests = 10,
    int rateLimitWindowSeconds = 60)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["CrashIngest:MaxPayloadBytes"] = maxPayloadBytes.ToString(),
                ["CrashIngest:RateLimitMaxRequests"] = rateLimitMaxRequests.ToString(),
                ["CrashIngest:RateLimitWindowSeconds"] = rateLimitWindowSeconds.ToString(),
                ["CrashIngest:RequireHttps"] = "true",
                ["CrashIngest:RecentMaxLimit"] = "100"
            };
            configBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(IDbContextFactory<ApplicationDbContext>));

            var databaseName = $"mobile-crash-tests-{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
