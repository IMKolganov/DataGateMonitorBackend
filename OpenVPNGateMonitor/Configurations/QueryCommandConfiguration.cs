using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;
using OpenVPNGateMonitor.DataBase.Services.Query.DeviceTable;
using OpenVPNGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;
using OpenVPNGateMonitor.DataBase.Services.Query.LocalizationTextTable;
using OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerStatusLogTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.DataBase.Services.Query.RoleTable;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;

namespace OpenVPNGateMonitor.Configurations;

public static class QueryCommandConfiguration
{
    public static void ConfigureQueryCommand(this IServiceCollection services)
    {
        // Generic services
        services.AddScoped(typeof(IQueryService<,>), typeof(EfQueryService<,>));
        services.AddScoped(typeof(ICommandService<,>), typeof(EfCommandService<,>));
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        
        services.AddScoped<IClientApplicationQueryService, ClientApplicationQueryService>();
        services.AddScoped<IIncomingMessageLogQueryService, IncomingMessageLogQueryService>();
        services.AddScoped<IIssuedOvpnFileQueryService, IssuedOvpnFileQueryService>();
        services.AddScoped<IIssuedOvpnFileTokenQueryService, IssuedOvpnFileTokenQueryService>();
        services.AddScoped<ILocalizationTextQueryService, LocalizationTextQueryService>();
        services.AddScoped<INotificationRecipientQueryService, NotificationRecipientQueryService>();

        services.AddScoped<IOpenVpnGeoQueryService, OpenVpnGeoQueryService>();
        services.AddScoped<IOpenVpnOverviewSeriesQuery, OpenVpnOverviewSeriesQuery>();
        services.AddScoped<IOpenVpnOverviewTotalsQuery, OpenVpnOverviewTotalsQuery>();
        services.AddScoped<IOpenVpnServerClientOverviewQuery, OpenVpnServerClientOverviewQuery>();
        services.AddScoped<IOpenVpnServerClientQueryService, OpenVpnServerClientQueryService>();
        services.AddScoped<IOpenVpnServerEventLogQueryService, OpenVpnServerEventLogQueryService>();
        services.AddScoped<IOpenVpnServerOvpnFileConfigQueryService, OpenVpnServerOvpnFileConfigQueryService>();
        services.AddScoped<IOpenVpnServerStatusLogQueryService, OpenVpnServerStatusLogQueryService>();
        services.AddScoped<IOpenVpnServerQueryService, OpenVpnServerQueryService>();
        services.AddScoped<ITelegramBotUserQueryService, TelegramBotUserQueryService>();
        services.AddScoped<ITelegramUserLanguagePreferenceQueryService, TelegramUserLanguagePreferenceQueryService>();
        
        
        services.AddScoped<IUserCredentialQueryService, UserCredentialQueryService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserIdentityLinkQueryService, UserIdentityLinkQueryService>();
        services.AddScoped<IQuotaPlanQueryService, QuotaPlanQueryService>();
        services.AddScoped<IUserRoleQueryService, UserRoleQueryService>();
        services.AddScoped<IRoleQueryService, RoleQueryService>();
        services.AddScoped<IUserQuotaPlanQueryService, UserQuotaPlanQueryService>();
        services.AddScoped<IQuotaPlanAllowedServerQueryService, QuotaPlanAllowedServerQueryService>();
        
        services.AddScoped<IUserRefreshTokenQueryService, UserRefreshTokenQueryService>();
        
        services.AddScoped<IDeviceQueryService, DeviceQueryService>();


        // Feature: overview queries
        services.AddScoped<IOpenVpnServerOverviewQuery, OpenVpnServerOverviewQuery>();

    }
}