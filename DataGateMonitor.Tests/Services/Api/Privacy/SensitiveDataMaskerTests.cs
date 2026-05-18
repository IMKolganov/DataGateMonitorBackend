using System.Security.Claims;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.Services.Api.Privacy;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Privacy;

public class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskIdentifier_IsStableAsterisksAndHidesRawValue()
    {
        var masked = SensitiveDataMasker.MaskIdentifier("5767006971");
        Assert.Matches(@"^\*{5,10}$", masked);
        Assert.DoesNotContain("5767006971", masked);
        Assert.Equal(masked, SensitiveDataMasker.MaskIdentifier("5767006971"));
    }

    [Fact]
    public void MaskDisplayName_UsesAsterisksForPlainNames()
    {
        var masked = SensitiveDataMasker.MaskDisplayName("Alice");
        Assert.Matches(@"^\*{5,10}$", masked);
    }

    [Fact]
    public void MaskCommonName_MasksCnAndEmail()
    {
        var cn = SensitiveDataMasker.MaskCommonName("tg-5767006971-3");
        Assert.StartsWith("cn-", cn);
        Assert.DoesNotContain("5767006971", cn);

        Assert.Equal("***@***", SensitiveDataMasker.MaskCommonName("user@example.com"));
    }

    [Fact]
    public void MaskFreeText_ReplacesEmbeddedEmail()
    {
        var masked = SensitiveDataMasker.MaskFreeText("contact user@example.com please");
        Assert.DoesNotContain("user@example.com", masked);
        Assert.Contains("***@***", masked);
    }
}

public class ClientStatisticsResponseSanitizerTests
{
    private static ClaimsPrincipal RegularUser => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, "User")]));

    private static ClaimsPrincipal AdminUser => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")]));

    [Fact]
    public void ApplyIfNeeded_NonPrivileged_MasksConnectedClients()
    {
        var response = new ConnectedClientsResponse
        {
            VpnClients =
            [
                new VpnClientInfoDto
                {
                    ExternalId = "5767006971",
                    CommonName = "tg-5767006971-3",
                    Username = "alice@example.com",
                    DisplayName = "Alice",
                    AvatarUrl = "https://cdn.example/avatar.png",
                },
            ],
        };

        ClientStatisticsResponseSanitizer.ApplyIfNeeded(RegularUser, response);

        var client = response.VpnClients![0];
        Assert.Matches(@"^\*{5,10}$", client.ExternalId);
        Assert.StartsWith("cn-", client.CommonName);
        Assert.Equal("***@***", client.Username);
        Assert.Matches(@"^\*{5,10}$", client.DisplayName);
        Assert.Null(client.AvatarUrl);
    }

    [Fact]
    public void ApplyIfNeeded_Admin_LeavesValuesUntouched()
    {
        var response = new ConnectedClientsResponse
        {
            VpnClients =
            [
                new VpnClientInfoDto
                {
                    ExternalId = "5767006971",
                    CommonName = "tg-5767006971-3",
                    DisplayName = "Alice",
                },
            ],
        };

        ClientStatisticsResponseSanitizer.ApplyIfNeeded(AdminUser, response);

        var client = response.VpnClients![0];
        Assert.Equal("5767006971", client.ExternalId);
        Assert.Equal("tg-5767006971-3", client.CommonName);
        Assert.Equal("Alice", client.DisplayName);
    }

    [Fact]
    public void ApplyIfNeeded_NonPrivileged_MasksTrafficByClients()
    {
        var response = new TrafficByClientsResponse
        {
            ClientTraffics =
            [
                new ClientTrafficDto
                {
                    ExternalId = "5767006971",
                    CommonName = "user@example.com",
                    TgUsername = "alice",
                    TgFirstName = "Alice",
                    TgLastName = "Smith",
                },
            ],
        };

        ClientStatisticsResponseSanitizer.ApplyIfNeeded(RegularUser, response);

        var item = response.ClientTraffics![0];
        Assert.Matches(@"^\*{5,10}$", item.ExternalId);
        Assert.Equal("***@***", item.CommonName);
        Assert.StartsWith("@", item.TgUsername);
        Assert.Matches(@"^\*{5,10}$", item.TgFirstName);
    }

    [Fact]
    public void ApplyIfNeeded_NonPrivileged_MasksOverviewUsers()
    {
        var response = new OverviewUsersResponse
        {
            OverviewUserItems =
            [
                new OverviewUserDto
                {
                    ExternalId = "google-oauth2|abc",
                    DisplayName = "Bob google-oauth2|abc",
                },
            ],
        };

        ClientStatisticsResponseSanitizer.ApplyIfNeeded(RegularUser, response);

        var user = response.OverviewUserItems![0];
        Assert.Matches(@"^\*{5,10}$", user.ExternalId);
        Assert.Matches(@"^\*{5,10}$", user.DisplayName);
        Assert.DoesNotContain("google-oauth2", user.DisplayName);
    }
}
