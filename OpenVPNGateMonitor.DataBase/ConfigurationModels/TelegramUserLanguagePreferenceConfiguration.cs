using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class TelegramUserLanguagePreferenceConfiguration : BaseEntityConfiguration<TelegramUserLanguagePreference, int>
{
    public override void Configure(EntityTypeBuilder<TelegramUserLanguagePreference> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.TelegramId).IsRequired();
        entity.Property(e => e.PreferredLanguage).IsRequired()
            .HasConversion<int>(); 
    }
}