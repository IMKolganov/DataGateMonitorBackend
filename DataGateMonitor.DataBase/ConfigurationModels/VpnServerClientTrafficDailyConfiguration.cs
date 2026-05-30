using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnServerClientTrafficDailyConfiguration : BaseEntityConfiguration<VpnServerClientTrafficDaily, int>
{
    public override void Configure(EntityTypeBuilder<VpnServerClientTrafficDaily> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.VpnServerId).IsRequired();
        entity.Property(e => e.ExternalId).HasMaxLength(255);
        entity.Property(e => e.SessionId).IsRequired();
        entity.Property(e => e.DayUtc).IsRequired();
        entity.Property(e => e.TrafficInBytes).IsRequired();
        entity.Property(e => e.TrafficOutBytes).IsRequired();
        entity.Property(e => e.SampleCount).IsRequired();

        entity.HasIndex(e => new { e.VpnServerId, e.SessionId, e.DayUtc })
            .IsUnique()
            .HasDatabaseName("UX_ClientTrafficDaily_Server_Session_Day");

        entity.HasIndex(e => new { e.DayUtc, e.VpnServerId })
            .HasDatabaseName("IX_ClientTrafficDaily_Day_Server");

        entity.HasIndex(e => new { e.DayUtc, e.ExternalId })
            .HasDatabaseName("IX_ClientTrafficDaily_Day_External");
    }
}
