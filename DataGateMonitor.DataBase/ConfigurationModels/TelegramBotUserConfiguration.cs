using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class TelegramBotUserConfiguration : BaseEntityConfiguration<TelegramBotUser, int>
{
    public override void Configure(EntityTypeBuilder<TelegramBotUser> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.TelegramId).IsRequired();
        entity.Property(e => e.Username).HasMaxLength(255);
        entity.Property(e => e.FirstName).HasMaxLength(255);
        entity.Property(e => e.LastName).HasMaxLength(255);
    }
}