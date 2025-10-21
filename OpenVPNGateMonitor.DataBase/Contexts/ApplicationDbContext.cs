using OpenVPNGateMonitor.DataBase.ConfigurationModels;
using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace OpenVPNGateMonitor.DataBase.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
    : DbContext(options)
{
    private readonly string _defaultSchema = (Environment.GetEnvironmentVariable("DB_DEFAULT_SCHEMA") 
                                              ?? configuration["DataBaseSettings:DefaultSchema"]) ?? "xgb_dashopnvpn";

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    public DbSet<OpenVpnServerStatusLog> OpenVpnServerStatusLogs { get; set; } = null!;
    public DbSet<OpenVpnServerClient> OpenVpnServerClients { get; set; } = null!;
    public DbSet<OpenVpnServer> OpenVpnServers { get; set; } = null!;
    public DbSet<IssuedOvpnFile> IssuedOvpnFiles { get; set; } = null!;
    public DbSet<IssuedOvpnFileToken> IssuedOvpnFileTokens { get; set; } = null!;
    public DbSet<OpenVpnServerOvpnFileConfig> OpenVpnServerOvpnFileConfigs { get; set; } = null!;
    public DbSet<ClientApplication> ClientApplications { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<TelegramBotUser> TelegramBotUsers { get; set; } = null!;
    public DbSet<TelegramUserLanguagePreference> TelegramUserLanguagePreferences { get; set; } = null!;
    public DbSet<LocalizationText> LocalizationTexts { get; set; } = null!;
    public DbSet<IncomingMessageLog> IncomingMessageLogs { get; set; } = null!;
    public DbSet<OpenVpnServerEventLog> OpenVpnServerEventLogs { get; set; } = null!;
    public DbSet<OpenVpnServerClientTraffic> OpenVpnServerClientTraffics { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<NotificationRecipient> NotificationRecipients { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserCredential> UserCredentials { get; set; } = null!;
    public DbSet<UserIdentityLink> UserIdentityLinks { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_defaultSchema);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new OpenVpnServerStatusLogConfiguration());
        modelBuilder.ApplyConfiguration(new OpenVpnServerClientConfiguration());
        modelBuilder.ApplyConfiguration(new OpenVpnServerConfiguration());
        modelBuilder.ApplyConfiguration(new IssuedOvpnFileConfiguration());
        modelBuilder.ApplyConfiguration(new IssuedOvpnFileTokenConfiguration());
        modelBuilder.ApplyConfiguration(new OpenVpnServerOvpnFileConfigConfiguration());
        modelBuilder.ApplyConfiguration(new ClientApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new SettingConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramBotUserConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramUserLanguagePreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new LocalizationTextConfiguration());
        modelBuilder.ApplyConfiguration(new IncomingMessageLogConfiguration());
        modelBuilder.ApplyConfiguration(new OpenVpnServerEventLogConfiguration());
        modelBuilder.ApplyConfiguration(new OpenVpnServerClientTrafficConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationRecipientConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserCredentialConfiguration());
        modelBuilder.ApplyConfiguration(new UserIdentityLinkConfiguration());

    }
    
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IBaseEntity)
            .ToList();

        foreach (var entry in entries)
        {
            var entity = (IBaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                var now = DateTimeOffset.UtcNow;
                entity.CreateDate = now;
                entity.LastUpdate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IBaseEntity.CreateDate)).IsModified = false;
                entity.LastUpdate = DateTimeOffset.UtcNow;
            }
        }
    }
}