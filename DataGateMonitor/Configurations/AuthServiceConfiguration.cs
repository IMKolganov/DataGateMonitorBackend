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