using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserVpnServerAccessRuleTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.VpnAccess;
using DataGateMonitor.SharedModels.DataGateMonitor.UserVpnServerAccessRules.Requests;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.Tests.Helpers;
using Moq;
using Xunit;

namespace DataGateMonitor.Tests.Services.VpnAccess;

public class UserVpnServerAccessRuleServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenMissing_AddsNewRule()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetByUserIdAndServerId(5, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVpnServerAccessRule?)null);

        var command = new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);
        command.Setup(c => c.Add(It.IsAny<UserVpnServerAccessRule>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVpnServerAccessRule e, bool _, CancellationToken __) =>
            {
                e.Id = 100;
                return e;
            });

        var sut = new UserVpnServerAccessRuleService(query.Object, command.Object);
        var response = await sut.CreateAsync(new CreateOrUpdateUserVpnServerAccessRuleRequest
        {
            UserId = 5,
            VpnServerId = 9,
            Mode = VpnServerAccessRuleMode.Allow
        }, CancellationToken.None);

        Assert.Equal(100, response.UserVpnServerAccessRule!.Id);
        Assert.Equal(VpnServerAccessRuleMode.Allow, response.UserVpnServerAccessRule.Mode);
        command.Verify(c => c.Add(It.IsAny<UserVpnServerAccessRule>(), true, It.IsAny<CancellationToken>()), Times.Once);
        command.Verify(c => c.Update(It.IsAny<UserVpnServerAccessRule>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenExistingDifferentMode_FlipsModeAndUpdates()
    {
        var existing = new UserVpnServerAccessRule
        {
            Id = 44,
            UserId = 5,
            VpnServerId = 9,
            Mode = VpnServerAccessRuleMode.Allow
        };

        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetByUserIdAndServerId(5, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);
        command.Setup(c => c.Update(existing, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new UserVpnServerAccessRuleService(query.Object, command.Object);
        var response = await sut.CreateAsync(new CreateOrUpdateUserVpnServerAccessRuleRequest
        {
            UserId = 5,
            VpnServerId = 9,
            Mode = VpnServerAccessRuleMode.Deny
        }, CancellationToken.None);

        Assert.Equal(44, response.UserVpnServerAccessRule!.Id);
        Assert.Equal(VpnServerAccessRuleMode.Deny, existing.Mode);
        command.Verify(c => c.Update(existing, true, It.IsAny<CancellationToken>()), Times.Once);
        command.Verify(c => c.Add(It.IsAny<UserVpnServerAccessRule>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenExistingSameMode_DoesNotUpdate()
    {
        var existing = new UserVpnServerAccessRule
        {
            Id = 44,
            UserId = 5,
            VpnServerId = 9,
            Mode = VpnServerAccessRuleMode.Deny
        };

        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetByUserIdAndServerId(5, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);

        var sut = new UserVpnServerAccessRuleService(query.Object, command.Object);
        var response = await sut.CreateAsync(new CreateOrUpdateUserVpnServerAccessRuleRequest
        {
            UserId = 5,
            VpnServerId = 9,
            Mode = VpnServerAccessRuleMode.Deny
        }, CancellationToken.None);

        Assert.Equal(44, response.UserVpnServerAccessRule!.Id);
        command.Verify(c => c.Update(It.IsAny<UserVpnServerAccessRule>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        command.Verify(c => c.Add(It.IsAny<UserVpnServerAccessRule>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPageAsync_ClampsPageAndMapsItems()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetPage(1, 20, null, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestPagedResult<UserVpnServerAccessRule>
            {
                Page = 1,
                PageSize = 20,
                TotalCount = 1,
                Items =
                [
                    new UserVpnServerAccessRule
                    {
                        Id = 3,
                        UserId = 5,
                        VpnServerId = 9,
                        Mode = VpnServerAccessRuleMode.Allow
                    }
                ]
            });

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        var result = await sut.GetPageAsync(new GetAllUserVpnServerAccessRulesRequest
        {
            Page = 0,
            PageSize = 0,
            VpnServerId = 9
        }, CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].UserId);
        query.VerifyAll();
    }

    [Fact]
    public async Task GetPageAsync_CapsPageSizeAt500()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetPage(2, 500, 5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestPagedResult<UserVpnServerAccessRule>
            {
                Page = 2,
                PageSize = 500,
                TotalCount = 0,
                Items = []
            });

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        var result = await sut.GetPageAsync(new GetAllUserVpnServerAccessRulesRequest
        {
            Page = 2,
            PageSize = 900,
            UserId = 5
        }, CancellationToken.None);

        Assert.Equal(500, result.PageSize);
        query.VerifyAll();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetById(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVpnServerAccessRule
            {
                Id = 7,
                UserId = 5,
                VpnServerId = 9,
                Mode = VpnServerAccessRuleMode.Deny
            });

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        var result = await sut.GetByIdAsync(7, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(7, result.UserVpnServerAccessRule.Id);
        Assert.Equal(VpnServerAccessRuleMode.Deny, result.UserVpnServerAccessRule.Mode);
        query.VerifyAll();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((UserVpnServerAccessRule?)null);

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        var result = await sut.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
        query.VerifyAll();
    }

    [Fact]
    public async Task GetListByUserIdAsync_MapsQueryResults()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetListByUserId(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserVpnServerAccessRule { Id = 1, UserId = 5, VpnServerId = 9, Mode = VpnServerAccessRuleMode.Allow }
            ]);

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        var result = await sut.GetListByUserIdAsync(5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(9, result[0].VpnServerId);
        query.VerifyAll();
    }

    [Fact]
    public async Task GetListByVpnServerIdAsync_MapsQueryResults()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetListByVpnServerId(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserVpnServerAccessRule { Id = 1, UserId = 5, VpnServerId = 9, Mode = VpnServerAccessRuleMode.Deny }
            ]);

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        var result = await sut.GetListByVpnServerIdAsync(9, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(5, result[0].UserId);
        query.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesFields()
    {
        var entity = new UserVpnServerAccessRule
        {
            Id = 4,
            UserId = 5,
            VpnServerId = 9,
            Mode = VpnServerAccessRuleMode.Allow
        };
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetById(4, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        query.Setup(q => q.GetByUserIdAndServerId(5, 11, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVpnServerAccessRule?)null);

        var command = new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);
        command.Setup(c => c.Update(entity, true, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new UserVpnServerAccessRuleService(query.Object, command.Object);
        await sut.UpdateAsync(new CreateOrUpdateUserVpnServerAccessRuleRequest
        {
            Id = 4,
            UserId = 5,
            VpnServerId = 11,
            Mode = VpnServerAccessRuleMode.Deny
        }, CancellationToken.None);

        Assert.Equal(11, entity.VpnServerId);
        Assert.Equal(VpnServerAccessRuleMode.Deny, entity.Mode);
        command.VerifyAll();
        query.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenIdMissing_Throws()
    {
        var sut = new UserVpnServerAccessRuleService(
            new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict).Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateAsync(
            new CreateOrUpdateUserVpnServerAccessRuleRequest { Id = 0, UserId = 5, VpnServerId = 9 },
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_Throws()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((UserVpnServerAccessRule?)null);

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UpdateAsync(
            new CreateOrUpdateUserVpnServerAccessRuleRequest { Id = 99, UserId = 5, VpnServerId = 9 },
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenPairBelongsToAnotherRule_Throws()
    {
        var query = new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict);
        query.Setup(q => q.GetById(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVpnServerAccessRule { Id = 4, UserId = 5, VpnServerId = 9 });
        query.Setup(q => q.GetByUserIdAndServerId(5, 11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVpnServerAccessRule { Id = 8, UserId = 5, VpnServerId = 11 });

        var sut = new UserVpnServerAccessRuleService(query.Object,
            new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict).Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateAsync(
            new CreateOrUpdateUserVpnServerAccessRuleRequest
            {
                Id = 4,
                UserId = 5,
                VpnServerId = 11,
                Mode = VpnServerAccessRuleMode.Allow
            },
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_Deletes()
    {
        var command = new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);
        command.Setup(c => c.DeleteById(4, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new UserVpnServerAccessRuleService(
            new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict).Object,
            command.Object);

        await sut.DeleteAsync(4, CancellationToken.None);

        command.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_Throws()
    {
        var command = new Mock<ICommandService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);
        command.Setup(c => c.DeleteById(99, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = new UserVpnServerAccessRuleService(
            new Mock<IUserVpnServerAccessRuleQueryService>(MockBehavior.Strict).Object,
            command.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.DeleteAsync(99, CancellationToken.None));
    }
}
