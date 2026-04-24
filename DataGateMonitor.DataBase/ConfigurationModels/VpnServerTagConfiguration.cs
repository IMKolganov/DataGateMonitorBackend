using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnServerTagConfiguration : IEntityTypeConfiguration<VpnServerTag>
{
    public void Configure(EntityTypeBuilder<VpnServerTag> entity)
    {
        entity.Property(e => e.TagId)
            .IsRequired();

        entity.Property(e => e.VpnServerId)
            .IsRequired();

        entity.HasKey(e => new { e.TagId, e.VpnServerId });

        entity.Property(e => e.CreateDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.LastUpdate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.HasIndex(e => e.TagId);
        entity.HasIndex(e => e.VpnServerId);
    }
}
