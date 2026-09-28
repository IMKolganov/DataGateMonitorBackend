using DataGateMonitor.DataBase.ConfigurationModels.Seeds;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Configuration;

public class LocalizationTextSeedDataTests
{
    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void DashboardLoginCode_exists_for_each_language_with_placeholders(Language language)
    {
        var text = GetText("DashboardLoginCode", language);

        Assert.Contains("{code}", text);
        Assert.Contains("{minutes}", text);
        Assert.Contains("<code>{code}</code>", text);
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void DashboardLoginCodeError_exists_for_each_language(Language language)
    {
        var text = GetText("DashboardLoginCodeError", language);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.DoesNotContain("{code}", text);
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void AccountLinkTelegramAlreadyLinkedToGoogle_exists_for_each_language_with_placeholders(Language language)
    {
        var text = GetText("AccountLinkTelegramAlreadyLinkedToGoogle", language);

        Assert.Contains("{accountLabel}", text);
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void VpnServerNotAllowedByQuotaPlan_exists_for_each_language(Language language)
    {
        var text = GetText("VpnServerNotAllowedByQuotaPlan", language);

        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void DonateIntro_exists_for_each_language(Language language)
    {
        var text = GetText("DonateIntro", language);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("Stars", text);
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void DonateStarsTitle_and_description_exist(Language language)
    {
        Assert.False(string.IsNullOrWhiteSpace(GetText("DonateStarsTitle", language)));
        Assert.False(string.IsNullOrWhiteSpace(GetText("DonateStarsDescription", language)));
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void DonateCryptoRiskBanner_warns_about_p2p(Language language)
    {
        var text = GetText("DonateCryptoRiskBanner", language);

        Assert.Contains("!!!", text);
        Assert.Contains("P2P", text);
        Assert.Contains("{amount}", text);
        Assert.False(string.IsNullOrWhiteSpace(GetText("DonateCryptoRiskContinue", language)));
        Assert.False(string.IsNullOrWhiteSpace(GetText("DonateCryptoRiskIgnore", language)));
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Greek)]
    [InlineData(Language.Russian)]
    public void DonatePayButton_and_thanks_have_placeholders(Language language)
    {
        Assert.Contains("{amount}", GetText("DonatePayButton", language));
        Assert.Contains("{amount}", GetText("DonateInvoiceCreated", language));
        Assert.Contains("{amount}", GetText("DonateThanks", language));
        Assert.Contains("{asset}", GetText("DonateThanks", language));
    }

    [Fact]
    public void BotMenu_includes_donate_command()
    {
        foreach (var language in new[] { Language.English, Language.Greek, Language.Russian })
            Assert.Contains("/donate", GetText("BotMenu", language));
    }

    [Fact]
    public void Seed_has_unique_ids()
    {
        var data = LocalizationTextSeedData.GetData();
        var ids = data.Select(x => x.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    private static string GetText(string key, Language language)
    {
        var entry = LocalizationTextSeedData.GetData()
            .SingleOrDefault(x => x.Key == key && x.Language == language);

        Assert.NotNull(entry);
        return entry!.Text;
    }
}
