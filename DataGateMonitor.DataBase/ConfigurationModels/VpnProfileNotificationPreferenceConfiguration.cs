using DataGateMonitor.DataBase.ConfigurationModels.Seeds;
using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnProfileNotificationPreferenceConfiguration : BaseEntityConfiguration<VpnProfileNotificationPreference, int>
{
    public override void Configure(EntityTypeBuilder<VpnProfileNotificationPreference> entity)
    {
        base.Configure(entity);

        entity.ToTable("VpnProfileNotificationPreferences");

        entity.Property(e => e.Kind)
            .IsRequired()
            .HasConversion<int>();

        entity.Property(e => e.Enabled)
            .IsRequired();

        entity.HasIndex(e => e.Kind)
            .IsUnique();

        entity.HasData(ApplicationNotificationPreferenceSeedData.Data);
    }
}
