using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.DataBase.ConfigurationModels.Seeds;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class QuotaPlanConfiguration : BaseEntityConfiguration<QuotaPlan, int>
{
    public override void Configure(EntityTypeBuilder<QuotaPlan> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.Description)
            .HasMaxLength(256);

        entity.Property(e => e.DailyQuotaBytes);
        entity.Property(e => e.MonthlyQuotaBytes);

        entity.Property(e => e.UpKbps);
        entity.Property(e => e.DownKbps);

        entity.Property(e => e.OverlimitAction)
            .HasConversion<int>();

        entity.Property(e => e.ThrottleUpKbps);
        entity.Property(e => e.ThrottleDownKbps);

        entity.Property(e => e.IsActive)
            .HasDefaultValue(true);

        entity.Property(e => e.IsDefault)
            .HasDefaultValue(false);

        // Indexes
        entity.HasIndex(e => e.Name)
            .IsUnique();

        entity.HasIndex(e => e.IsDefault);

        // Seed data
        entity.HasData(QuotaPlanSeedData.Data);
    }
}