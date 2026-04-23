using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using DataGateMonitor.Controllers;
using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Api.Auth;
using DataGateMonitor.Services.Api.Auth.Handlers;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.Api.Auth.ForgotPassword;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.TelegramLogin;
using DataGateMonitor.Services.Api.Auth.Registers;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.Auth.Users;
using DataGateMonitor.Services.Api.CurrentUser;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;

namespace DataGateMonitor.Configurations;

public static class AuthServiceConfiguration
{
    public static void ConfigureAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        services.AddScoped<IUserLoginService, UserLoginService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IGoogleAuthCodeExchangeService, GoogleAuthCodeExchangeService>();
        #region example google env
        // GoogleAuth:ClientId → ENV: GOOGLEAUTH__CLIENTID
        // GoogleAuth:ClientSecret → ENV: GOOGLEAUTH__CLIENTSECRET
        #endregion
        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuth"));
        
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        
        services.AddScoped<IUserQuotaPlanService, UserQuotaPlanService>();

        services.AddMemoryCache();
        services.AddScoped<IAdminForgotPasswordService, AdminForgotPasswordService>();
        services.AddScoped<ITelegramLoginCodeService, TelegramLoginCodeService>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOrOwnServer", policy =>
                policy.Requirements.Add(new AdminOrOwnServerRequirement()));
        });

        services.AddScoped<IVpnServerAccessQueryService, VpnServerAccessQueryService>();
        services.AddScoped<IAuthorizationHandler, AdminOrOwnServerHandler>();

        services.AddScoped<IDeviceService, DeviceService>();

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

                // SharedModels: avoid short-name collisions. FullName alone breaks OpenAPI 3.0.3 (keys must match /^[a-zA-Z0-9.\-_]+$/) because .NET generic FullName contains ` [ ] , = etc.
                options.CustomSchemaIds(ToOpenApiComponentSchemaId);

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

    /// <summary>
    /// OpenAPI 3.0.3: <c>components.schemas</c> keys must match <c>/^[a-zA-Z0-9.\-_]+$/</c>.
    /// .NET <see cref="Type.FullName"/> for generics uses backticks and brackets (e.g. <c>ApiResponse`1[[T,...]]</c>), which Orval rejects.
    /// </summary>
    private static string ToOpenApiComponentSchemaId(Type type)
    {
        var raw = type.FullName ?? type.Name;
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (char.IsAsciiLetter(ch) || char.IsAsciiDigit(ch) || ch is '.' or '-' or '_')
                sb.Append(ch);
            else if (ch == '+')
                sb.Append('.'); // nested type separator in CLR metadata
            else
                sb.Append('_');
        }

        var id = sb.ToString();
        while (id.Contains("__", StringComparison.Ordinal))
            id = id.Replace("__", "_", StringComparison.Ordinal);
        while (id.Contains("..", StringComparison.Ordinal))
            id = id.Replace("..", ".", StringComparison.Ordinal);
        id = id.Trim('_', '.');
        return id.Length > 0 ? id : type.Name;
    }
}