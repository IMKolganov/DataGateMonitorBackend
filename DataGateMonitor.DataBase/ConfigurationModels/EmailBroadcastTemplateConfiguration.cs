using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class EmailBroadcastTemplateConfiguration : BaseEntityConfiguration<EmailBroadcastTemplate, int>
{
    public override void Configure(EntityTypeBuilder<EmailBroadcastTemplate> entity)
    {
        base.Configure(entity);

        entity.ToTable("EmailBroadcastTemplates");

        entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
        entity.Property(e => e.Description).HasMaxLength(512);
        entity.Property(e => e.Subject).IsRequired().HasMaxLength(512);
        entity.Property(e => e.BodyHtml).IsRequired().HasColumnType("text");

        entity.HasIndex(e => e.Name).IsUnique();
    }
}
