using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class QuotaPlanAllowedServerConfiguration : IEntityTypeConfiguration<QuotaPlanAllowedServer>
{
    public void Configure(EntityTypeBuilder<QuotaPlanAllowedServer> entity)
    {
        entity.Property(e => e.QuotaPlanId)
            .IsRequired();

        entity.Property(e => e.VpnServerId)
            .IsRequired();
        
        // Composite PK
        entity.HasKey(e => new { e.QuotaPlanId, e.VpnServerId });

        // Index for quick filtering by plan
        entity.HasIndex(e => e.QuotaPlanId);
    }
}