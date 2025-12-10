using System.Reflection;
using OpenVPNGateMonitor.Configurations;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureSerilog();

// 🔐
var logger = Log.ForContext("SourceContext", "JwtSecretLoader");
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
logger.Information("DOTNET_RUNNING_IN_CONTAINER = {IsDocker}", isDocker);
logger.Information($"Application version: {version};");

var jwtSecret = JwtSecretLoaderConfiguration.LoadOrGenerateSecret(logger);
builder.Configuration["Jwt:Secret"] = jwtSecret;

#region google secret
builder.Configuration
    .AddJsonFile("secrets/googleauth.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Optional fallback secret (NOT clientId)
var googleAuthSecret = GoogleAuthSecretLoaderConfiguration.LoadSecret(logger);

if (!string.IsNullOrEmpty(googleAuthSecret))
{
    builder.Configuration["GoogleAuth:Secret"] = googleAuthSecret;
}
#endregion


builder.Services.ConfigureServices(builder.Configuration);
builder.Services.ConfigureQueryCommand();
builder.Services.ConfigureTelegramServices();
builder.Services.ConfigureGeoLiteServices();
builder.Services.ConfigureAuthServices(builder.Configuration);
builder.Services.DataBaseServices(builder.Configuration, logger);
builder.Services.ConfigureJwt(builder.Configuration);
builder.Services.ConfigureMapster();
builder.Services.ConfigureNotificationServices();
builder.Services.ConfigureHealthCheckServices(builder.Configuration);

builder.ConfigureWebHost();
builder.ConfigureExternalIpServices();
builder.Services.AddOpenApi();

var app = builder.Build();
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

app.ConfigureMiddleware();
app.ConfigurePipeline();

app.Run();