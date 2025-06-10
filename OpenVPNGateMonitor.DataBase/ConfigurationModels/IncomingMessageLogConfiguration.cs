using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class IncomingMessageLogConfiguration : BaseEntityConfiguration<IncomingMessageLog>
{
    public override void Configure(EntityTypeBuilder<IncomingMessageLog> entity)
    {
        base.Configure(entity);
        
        entity.Property(e => e.TelegramId)
            .IsRequired();

        entity.Property(e => e.Username)
            .HasMaxLength(128);

        entity.Property(e => e.FirstName)
            .HasMaxLength(128);

        entity.Property(e => e.LastName)
            .HasMaxLength(128);

        entity.Property(e => e.MessageText)
            .IsRequired()
            .HasMaxLength(4000);

        entity.Property(e => e.FileType)
            .HasMaxLength(32);

        entity.Property(e => e.FileId)
            .HasMaxLength(512);

        entity.Property(e => e.FileName)
            .HasMaxLength(512);

        entity.Property(e => e.FilePath)
            .HasMaxLength(1024);

        entity.Property(e => e.FileSize);

        entity.Property(e => e.ReceivedAt)
            .IsRequired();

        // Indexes
        entity.HasIndex(e => e.TelegramId);
        entity.HasIndex(e => e.ReceivedAt);
    }
}