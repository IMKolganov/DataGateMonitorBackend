using Microsoft.OpenApi;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Services.Api.Auth;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Configurations;

public static class AuthServiceConfiguration
{
    public static void ConfigureAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        #region example google env
        // GoogleAuth:ClientId → ENV: GOOGLEAUTH__CLIENTID
        // GoogleAuth:ClientSecret → ENV: GOOGLEAUTH__CLIENTSECRET
        #endregion
        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuth"));

        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        services.AddAuthorization();

        services.AddControllers(options =>
            {
                options.Filters.Add<ValidationFilter>();
            })
            .AddNewtonsoftJson();

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "OpenVPN Gate Monitor API",
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter the token in the format 'Bearer {your_token}'"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            })
            .AddSwaggerGenNewtonsoftSupport();
    }
}