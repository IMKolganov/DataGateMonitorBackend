using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Writers;
using OpenVPNGateMonitor.DataBase.Contexts;
using OpenVPNGateMonitor.Hubs;
using Swashbuckle.AspNetCore.Swagger;

namespace OpenVPNGateMonitor.Configurations;

public static class PipelineConfiguration
{
    public static void ConfigurePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost,
        });

        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.ContainsKey("X-Forwarded-Host"))
            {
                var host = context.Request.Headers["X-Forwarded-Host"].ToString();
                context.Request.Host = new HostString(host);
            }

            if (context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
            {
                var scheme = context.Request.Headers["X-Forwarded-Proto"].ToString();
                context.Request.Scheme = scheme;
            }

            await next();
        });

        app.UseWebSockets();
        app.UseCors("AllowAllOriginsWithCredentials");

        if (app.Environment.IsDevelopment())
        {
            // app.MapOpenApi();
            app.UseSwagger(o =>
            {
                // IMPORTANT: allow both .json and .yaml on the same route
                o.RouteTemplate = "swagger/{documentName}/swagger.{json|yaml}";
            });
            app.UseSwaggerUI(o =>
            {
                o.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
                o.RoutePrefix = "swagger";
            });
        }

        // app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        // Apply EF Core migrations with detailed per-migration logging
        app.ApplyMigrationsWithDetailedLogging<ApplicationDbContext>();

        app.MapControllers();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResultStatusCodes = {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.MapGet("/error/404", () => Results.Problem(statusCode: 404, title: "Page Not Found", 
                detail: "The requested resource was not found."))
            .ExcludeFromDescription();

        app.MapGet("/swaggerjson", async context =>
        {
            var provider = context.RequestServices.GetRequiredService<ISwaggerProvider>();
            var swaggerDoc = provider.GetSwagger("v1");

            context.Response.ContentType = "application/json";

            var stringWriter = new StringWriter();
            var jsonWriter = new OpenApiJsonWriter(stringWriter);
            swaggerDoc.SerializeAsV3(jsonWriter);

            await context.Response.WriteAsync(stringWriter.ToString());
        });

        app.MapHub<GeoLiteHub>("/api/hubs/geoLite");
        app.MapHub<OpenVpnFrontendHub>("/api/hubs/frontend");
        app.MapHub<AdminNotificationHub>("/hubs/admin-notify");

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
        var environmentName = app.Environment.EnvironmentName;

        app.MapGet("/",
            () => Results.Text(statusCode: 200, 
                content: $"OpenVPNGateMonitor Application version: {version}; Environment: {environmentName};"));

        app.Logger.LogInformation($"Application version: {version}; Environment: {environmentName};");

        RsaKeyInitializer.EnsureRsaKeysExist(app.Configuration, app.Logger);
    }
}