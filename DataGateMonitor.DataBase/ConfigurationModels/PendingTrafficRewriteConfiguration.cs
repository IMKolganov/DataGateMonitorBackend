using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class PendingTrafficRewriteConfiguration : BaseEntityConfiguration<PendingTrafficRewrite, long>
{
    public override void Configure(EntityTypeBuilder<PendingTrafficRewrite> entity)
    {
        base.Configure(entity);

        entity.ToTable("PendingTrafficRewrites");

        entity.Property(e => e.OldExternalId).IsRequired().HasMaxLength(255);
        entity.Property(e => e.NewExternalId).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Status).IsRequired();
        entity.Property(e => e.LastProcessedChunkName).HasMaxLength(256);
        entity.Property(e => e.LastError).HasMaxLength(2048);
        entity.Property(e => e.RowsUpdated).IsRequired();

        entity.HasIndex(e => e.Status)
            .HasDatabaseName("IX_PendingTrafficRewrites_Status");

        entity.HasIndex(e => new { e.Status, e.Id })
            .HasDatabaseName("IX_PendingTrafficRewrites_Status_Id");
    }
}
