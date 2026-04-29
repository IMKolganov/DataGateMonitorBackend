using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class SentEmailLogConfiguration : BaseEntityConfiguration<SentEmailLog, int>
{
    public override void Configure(EntityTypeBuilder<SentEmailLog> entity)
    {
        base.Configure(entity);

        entity.ToTable("SentEmailLogs");

        entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(256);
        entity.Property(e => e.Subject).IsRequired().HasMaxLength(512);
        entity.Property(e => e.BodyHtml).IsRequired().HasColumnType("text");
        entity.Property(e => e.Success).IsRequired();
        entity.Property(e => e.ErrorMessage).HasMaxLength(4000);

        entity.HasIndex(e => e.CreateDate);
        entity.HasIndex(e => e.RecipientUserId);
    }
}
