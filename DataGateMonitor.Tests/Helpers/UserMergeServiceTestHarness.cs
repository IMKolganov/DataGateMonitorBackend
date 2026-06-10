using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories;
using DataGateMonitor.DataBase.Repositories.Queries;
using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Users;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using DataGateMonitor.SharedModels.Enums;
using Moq;

namespace DataGateMonitor.Tests.Helpers;

/// <summary>In-memory EF harness for <see cref="UserMergeService"/> integration tests.</summary>
internal sealed class UserMergeServiceTestHarness : IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context => _context;
    public UserMergeService Service { get; }

    private UserMergeServiceTestHarness(ApplicationDbContext context, UserMergeService service, SqliteConnection connection)
    {
        _context = context;
        Service = service;
        _connection = connection;
    }

    public static UserMergeServiceTestHarness Create()
    {
        // SQLite in-memory supports ExecuteUpdate/ExecuteDelete (required by UserMergeService).
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(b => b.Ignore(RelationalEventId.AmbientTransactionWarning))
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataBaseSettings:DefaultSchema"] = "test_schema",
            })
            .Build();

        var context = new ApplicationDbContext(options, configuration);
        context.Database.EnsureCreated();
        var repositoryFactory = new RepositoryFactory(context);
        var queryFactory = new QueryFactory(context);
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        var unitOfWork = new UnitOfWork(context, dbContextFactory.Object, repositoryFactory, queryFactory);

        IQueryService<User, int> userQueryCore = new EfQueryService<User, int>(unitOfWork);
        IQueryService<UserIdentityLink, int> linkQueryCore = new EfQueryService<UserIdentityLink, int>(unitOfWork);

        var userQueryService = new UserQueryService(userQueryCore, linkQueryCore);
        var identityLinkQueryService = new UserIdentityLinkQueryService(linkQueryCore);
        var userCommandService = new EfCommandService<User, int>(unitOfWork);
        var archiveCommandService = new EfCommandService<MergedUserArchive, int>(unitOfWork);

        var service = new UserMergeService(
            unitOfWork,
            userQueryService,
            identityLinkQueryService,
            userCommandService,
            archiveCommandService,
            NullLogger<UserMergeService>.Instance);

        return new UserMergeServiceTestHarness(context, service, connection);
    }

    public async Task<(User TelegramUser, User GoogleUser)> SeedTelegramGooglePairAsync(
        string telegramExternalId = "111222333",
        string googleExternalId = "google-sub-abc",
        string? googleEmail = "user@gmail.com",
        string? telegramEmail = null,
        bool googleIsAdmin = false,
        bool googleHasDashboardAccess = true,
        string googleProvider = "google",
        string telegramProvider = "telegram")
    {
        var now = DateTimeOffset.UtcNow;

        var telegramUser = new User
        {
            DisplayName = "tg_user",
            Email = telegramEmail,
            HasDashboardAccess = false,
            CreateDate = now,
            LastUpdate = now,
        };

        var googleUser = new User
        {
            DisplayName = "google_user",
            Email = googleEmail,
            IsEmailConfirmed = true,
            AvatarUrl = "https://example.com/avatar.png",
            HasDashboardAccess = googleHasDashboardAccess,
            IsAdmin = googleIsAdmin,
            CreateDate = now,
            LastUpdate = now,
        };

        _context.Users.AddRange(telegramUser, googleUser);
        await _context.SaveChangesAsync();

        _context.UserIdentityLinks.AddRange(
            new UserIdentityLink
            {
                UserId = telegramUser.Id,
                Provider = telegramProvider,
                ExternalId = telegramExternalId,
                CreateDate = now,
                LastUpdate = now,
            },
            new UserIdentityLink
            {
                UserId = googleUser.Id,
                Provider = googleProvider,
                ExternalId = googleExternalId,
                CreateDate = now,
                LastUpdate = now,
            });

        await _context.SaveChangesAsync();
        return (telegramUser, googleUser);
    }

    public async Task<UserIdentityLink> SeedIdentityLinkAsync(int userId, string provider, string externalId)
    {
        var now = DateTimeOffset.UtcNow;
        var link = new UserIdentityLink
        {
            UserId = userId,
            Provider = provider,
            ExternalId = externalId,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.UserIdentityLinks.Add(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<IssuedOvpnFile> SeedIssuedOvpnFileAsync(string externalId, int vpnServerId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var file = new IssuedOvpnFile
        {
            VpnServerId = vpnServerId,
            ExternalId = externalId,
            CommonName = $"cn-{externalId}",
            FileName = "client.ovpn",
            FilePath = "/tmp/client.ovpn",
            IssuedAt = now,
            IssuedTo = "test",
            PemFilePath = "/tmp/client.pem",
            CertFilePath = "/tmp/client.crt",
            KeyFilePath = "/tmp/client.key",
            ReqFilePath = "/tmp/client.req",
            CreateDate = now,
            LastUpdate = now,
        };

        _context.IssuedOvpnFiles.Add(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task<IssuedXrayClientLink> SeedIssuedXrayClientLinkAsync(string externalId, int vpnServerId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var link = new IssuedXrayClientLink
        {
            VpnServerId = vpnServerId,
            ExternalId = externalId,
            CommonName = $"cn-{externalId}",
            FileName = "client.txt",
            FilePath = "/tmp/client.txt",
            IssuedAt = now,
            IssuedTo = "test",
            PemFilePath = "/tmp/client.pem",
            CertFilePath = "/tmp/client.crt",
            KeyFilePath = "/tmp/client.key",
            ReqFilePath = "/tmp/client.req",
            CreateDate = now,
            LastUpdate = now,
        };

        _context.IssuedXrayClientLinks.Add(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<UserCredential> SeedCredentialAsync(int userId, string login)
    {
        var now = DateTimeOffset.UtcNow;
        var credential = new UserCredential
        {
            UserId = userId,
            Login = login,
            NormalizedLogin = login.ToUpperInvariant(),
            PasswordHash = "hash",
            PasswordUpdatedAt = DateTime.UtcNow,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.UserCredentials.Add(credential);
        await _context.SaveChangesAsync();
        return credential;
    }

    public async Task<UserQuotaPlan> SeedActiveQuotaPlanAsync(int userId, int quotaPlanId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var plan = new UserQuotaPlan
        {
            UserId = userId,
            QuotaPlanId = quotaPlanId,
            EffectiveFrom = now.AddDays(-1),
            EffectiveTo = null,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.UserQuotaPlans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<UserQuotaPlan> SeedHistoricalQuotaPlanAsync(int userId, int quotaPlanId = 2)
    {
        var now = DateTimeOffset.UtcNow;
        var plan = new UserQuotaPlan
        {
            UserId = userId,
            QuotaPlanId = quotaPlanId,
            EffectiveFrom = now.AddDays(-30),
            EffectiveTo = now.AddDays(-1),
            CreateDate = now,
            LastUpdate = now,
        };
        _context.UserQuotaPlans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task SeedUserRoleAsync(int userId, int roleId)
    {
        var now = DateTimeOffset.UtcNow;
        _context.Set<UserRole>().Add(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            CreateDate = now,
            LastUpdate = now,
        });
        await _context.SaveChangesAsync();
    }

    public async Task<Device> SeedDeviceAsync(int userId, string installationId = "device-1")
    {
        var now = DateTimeOffset.UtcNow;
        var device = new Device
        {
            UserId = userId,
            InstallationId = installationId,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.Set<Device>().Add(device);
        await _context.SaveChangesAsync();
        return device;
    }

    public async Task<UserRefreshToken> SeedRefreshTokenAsync(int userId, string tokenHash = "hash-token")
    {
        var now = DateTimeOffset.UtcNow;
        var token = new UserRefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
            CreateDate = now,
            LastUpdate = now,
        };
        _context.Set<UserRefreshToken>().Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<Notification> SeedNotificationAsync(int? actorUserId)
    {
        var now = DateTimeOffset.UtcNow;
        var notification = new Notification
        {
            Type = "test.event",
            Title = "title",
            Message = "message",
            ActorUserId = actorUserId,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<NotificationRecipient> SeedNotificationRecipientAsync(int notificationId, int adminUserId)
    {
        var now = DateTimeOffset.UtcNow;
        var recipient = new NotificationRecipient
        {
            NotificationId = notificationId,
            AdminUserId = adminUserId,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.Set<NotificationRecipient>().Add(recipient);
        await _context.SaveChangesAsync();
        return recipient;
    }

    public async Task<SentEmailLog> SeedSentEmailLogAsync(int? recipientUserId, int? sentByUserId)
    {
        var now = DateTimeOffset.UtcNow;
        var log = new SentEmailLog
        {
            RecipientUserId = recipientUserId,
            RecipientEmail = "test@example.com",
            Subject = "subject",
            BodyHtml = "<p>body</p>",
            Success = true,
            SentByUserId = sentByUserId,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.SentEmailLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<EmailBroadcastTemplate> SeedEmailTemplateAsync(int? createdByUserId)
    {
        var now = DateTimeOffset.UtcNow;
        var template = new EmailBroadcastTemplate
        {
            Name = "tpl",
            Subject = "subject",
            BodyHtml = "<p>body</p>",
            CreatedByUserId = createdByUserId,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.Set<EmailBroadcastTemplate>().Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<VpnServerClient> SeedVpnServerClientAsync(int? userId, string externalId, int vpnServerId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var client = new VpnServerClient
        {
            VpnServerId = vpnServerId,
            UserId = userId,
            ExternalId = externalId,
            SessionId = Guid.NewGuid(),
            CommonName = $"cn-{externalId}",
            RemoteIp = "1.2.3.4",
            LocalIp = "10.8.0.2",
            ConnectedSince = now,
            Username = externalId,
            IsConnected = true,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.VpnServerClients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<VpnServerClientTraffic> SeedVpnServerClientTrafficAsync(
        int? userId,
        string externalId,
        int vpnServerId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var traffic = new VpnServerClientTraffic
        {
            VpnServerId = vpnServerId,
            UserId = userId,
            ExternalId = externalId,
            SessionId = Guid.NewGuid(),
            BytesReceived = 100,
            BytesSent = 200,
            MeasuredAt = now,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.VpnServerClientTraffics.Add(traffic);
        await _context.SaveChangesAsync();
        return traffic;
    }

    public async Task<VpnServerClientTrafficDaily> SeedVpnServerClientTrafficDailyAsync(
        int? userId,
        string externalId,
        int vpnServerId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var daily = new VpnServerClientTrafficDaily
        {
            VpnServerId = vpnServerId,
            UserId = userId,
            ExternalId = externalId,
            SessionId = Guid.NewGuid(),
            DayUtc = DateOnly.FromDateTime(now.UtcDateTime),
            TrafficInBytes = 50,
            TrafficOutBytes = 75,
            SampleCount = 3,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.VpnServerClientTrafficDailies.Add(daily);
        await _context.SaveChangesAsync();
        return daily;
    }

    public async Task<MergedUserArchive> SeedArchiveForUserAsync(int originalUserId, int mergedIntoUserId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var archive = new MergedUserArchive
        {
            OriginalUserId = originalUserId,
            MergedIntoUserId = mergedIntoUserId,
            MergedAt = now,
            DisplayName = "archived",
            IdentityLinksJson = "[]",
            MergeReportJson = "{}",
            OriginalCreateDate = now,
            OriginalLastUpdate = now,
            CreateDate = now,
            LastUpdate = now,
        };
        _context.MergedUserArchives.Add(archive);
        await _context.SaveChangesAsync();
        return archive;
    }

    public async Task<MergeTelegramGoogleUsersResponse> MergeAsync(
        User telegram,
        User google,
        bool dryRun = false,
        string? note = null,
        int performedByUserId = 1)
    {
        // Production loads users with AsNoTracking; clear seed tracking before merge.
        var telegramUserId = telegram.Id;
        var googleUserId = google.Id;
        _context.ChangeTracker.Clear();

        return await Service.MergeTelegramGoogleAsync(
            new MergeTelegramGoogleUsersRequest
            {
                TelegramUserId = telegramUserId,
                GoogleUserId = googleUserId,
                DryRun = dryRun,
                Note = note,
            },
            performedByUserId,
            CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
