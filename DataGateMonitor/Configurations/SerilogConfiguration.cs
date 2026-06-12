using DataGateMonitor.Models.Helpers;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataGateMonitor.Configurations;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(this IHostBuilder host)
    {
        var elasticConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("elasticsearch.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning)
            .Filter.ByExcluding(IsExpiredAccessTokenNoise)
            .WriteTo.Console()
            .Enrich.FromLogContext();
        
        var elasticsearchSettings = new ElasticsearchSettings
        {
            Uri = (Environment.GetEnvironmentVariable("ELASTIC_URI") 
                   ?? elasticConfig["Elasticsearch:Uri"]) ?? string.Empty,

            Username = (Environment.GetEnvironmentVariable("ELASTIC_USERNAME") 
                        ?? elasticConfig["Elasticsearch:Username"]) ?? string.Empty,

            Password = (Environment.GetEnvironmentVariable("ELASTIC_PASSWORD") 
                        ?? elasticConfig["Elasticsearch:Password"]) ?? string.Empty,

            IndexFormat = (Environment.GetEnvironmentVariable("ELASTIC_INDEX_FORMAT") 
                           ?? elasticConfig["Elasticsearch:IndexFormat"]) ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(elasticsearchSettings.Uri))
        {
            loggerConfig = loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchSettings.Uri))
            {
                IndexFormat = elasticsearchSettings.IndexFormat ?? "default-index",
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
                NumberOfShards = 1,
                NumberOfReplicas = 0,
                ModifyConnectionSettings = conn => conn
                    .ServerCertificateValidationCallback((sender, cert, chain, errors) => true)
                    .BasicAuthentication(elasticsearchSettings.Username, elasticsearchSettings.Password),
                FailureCallback = (logEvent, exception) =>
                {
                    Console.WriteLine($"Unable to submit event: {logEvent.RenderMessage()}");
                    if (exception != null)
                        Console.WriteLine($"Exception: {exception.Message}");
                },
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
            });
        }

        Log.Logger = loggerConfig.CreateLogger();

        var serilogLogger = Log.ForContext(typeof(SerilogConfiguration));

        if (!string.IsNullOrWhiteSpace(elasticsearchSettings.Uri))
        {
            serilogLogger.Information($"📡 Elasticsearch logging is enabled. " +
                                      $"Host: {elasticsearchSettings.Uri}, " +
                                      $"IndexFormat: {elasticsearchSettings.IndexFormat}");
        }
        else
        {
            serilogLogger.Warning("🚨 Elasticsearch settings not found. Logging to console only.");
        }

        host.UseSerilog();
    }

    private static bool IsExpiredAccessTokenNoise(LogEvent logEvent)
    {
        if (logEvent.Exception is SecurityTokenExpiredException or SecurityTokenNotYetValidException)
            return true;

        var rendered = logEvent.RenderMessage();
        return rendered.Contains("IDX10223", StringComparison.OrdinalIgnoreCase)
               || rendered.Contains("IDX10225", StringComparison.OrdinalIgnoreCase)
               || rendered.Contains("SecurityTokenExpiredException", StringComparison.OrdinalIgnoreCase);
    }
}