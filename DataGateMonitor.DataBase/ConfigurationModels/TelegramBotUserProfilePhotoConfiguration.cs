using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class TelegramBotUserProfilePhotoConfiguration : BaseEntityConfiguration<TelegramBotUserProfilePhoto, int>
{
    public override void Configure(EntityTypeBuilder<TelegramBotUserProfilePhoto> entity)
    {
        base.Configure(entity);

        entity.ToTable("TelegramBotUserProfilePhotos");

        entity.Property(e => e.TelegramBotUserId).IsRequired();

        entity.Property(e => e.ImageBytes)
            .IsRequired();

        entity.Property(e => e.MimeType)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.TelegramFileUniqueId)
            .HasMaxLength(128);

        entity.HasIndex(e => e.TelegramBotUserId)
            .IsUnique();

        entity.HasOne(e => e.TelegramBotUser)
            .WithMany()
            .HasForeignKey(e => e.TelegramBotUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
