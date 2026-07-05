using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories;
using DataGateMonitor.DataBase.Repositories.Queries;
using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Users;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

namespace DataGateMonitor.Tests.Services.Users;

public class UserMergeServiceRollbackTests
{
    [Fact]
    public async Task MergeAsync_RollsBackTransaction_WhenDeleteFails()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(b => b.Ignore(RelationalEventId.AmbientTransactionWarning))
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataBaseSettings:DefaultSchema"] = "test_schema" })
            .Build();

        await using var context = new ApplicationDbContext(options, configuration);
        context.Database.EnsureCreated();

        var repositoryFactory = new RepositoryFactory(context);
        var queryFactory = new QueryFactory(context);
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        var unitOfWork = new UnitOfWork(context, dbContextFactory.Object, repositoryFactory, queryFactory);

        IQueryService<User, int> userQueryCore = new EfQueryService<User, int>(unitOfWork);
        IQueryService<UserIdentityLink, int> linkQueryCore = new EfQueryService<UserIdentityLink, int>(unitOfWork);

        var userQueryService = new UserQueryService(userQueryCore, linkQueryCore);
        var identityLinkQueryService = new UserIdentityLinkQueryService(linkQueryCore);
        var archiveCommandService = new EfCommandService<MergedUserArchive, int>(unitOfWork);

        var userCommandMock = new Mock<ICommandService<User, int>>();
        userCommandMock
            .Setup(c => c.Update(It.IsAny<User>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        userCommandMock
            .Setup(c => c.Delete(It.IsAny<User>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated delete failure"));

        var complianceMock = new Mock<IFreeTierAccessComplianceService>();

        var service = new UserMergeService(
            unitOfWork,
            userQueryService,
            identityLinkQueryService,
            userCommandMock.Object,
            archiveCommandService,
            complianceMock.Object,
            NullLogger<UserMergeService>.Instance);

        // Seed directly in our context
        var now = DateTimeOffset.UtcNow;
        var telegram = new User { DisplayName = "tg", CreateDate = now, LastUpdate = now };
        var google = new User { DisplayName = "google", Email = "g@test.com", CreateDate = now, LastUpdate = now };
        context.Users.AddRange(telegram, google);
        await context.SaveChangesAsync();
        context.UserIdentityLinks.AddRange(
            new UserIdentityLink { UserId = telegram.Id, Provider = "telegram", ExternalId = "111", CreateDate = now, LastUpdate = now },
            new UserIdentityLink { UserId = google.Id, Provider = "google", ExternalId = "google-sub", CreateDate = now, LastUpdate = now });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MergeTelegramGoogleAsync(
                new MergeTelegramGoogleUsersRequest { TelegramUserId = telegram.Id, GoogleUserId = google.Id },
                performedByUserId: 1,
                CancellationToken.None));

        Assert.Contains("simulated delete failure", ex.Message);
        Assert.Equal(2, await context.Users.CountAsync());
        Assert.Empty(await context.MergedUserArchives.ToListAsync());

        await context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
