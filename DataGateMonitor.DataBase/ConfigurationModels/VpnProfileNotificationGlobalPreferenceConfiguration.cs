using DataGateMonitor.DataBase.ConfigurationModels.Seeds;
using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnProfileNotificationGlobalPreferenceConfiguration : BaseEntityConfiguration<VpnProfileNotificationGlobalPreference, int>
{
    public override void Configure(EntityTypeBuilder<VpnProfileNotificationGlobalPreference> entity)
    {
        base.Configure(entity);

        entity.ToTable("VpnProfileNotificationGlobalPreferences");

        entity.Property(e => e.GloballyEnabled)
            .IsRequired();

        entity.HasData(VpnProfileNotificationGlobalPreferenceSeedData.Data);
    }
}
