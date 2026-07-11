using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class FreeTierDisconnectLogConfiguration : BaseEntityConfiguration<FreeTierDisconnectLog, int>
{
    public override void Configure(EntityTypeBuilder<FreeTierDisconnectLog> entity)
    {
        base.Configure(entity);

        entity.ToTable("FreeTierDisconnectLogs");

        entity.Property(e => e.CommonName).IsRequired().HasMaxLength(256);
        entity.Property(e => e.UserDisplayNameSnapshot).HasMaxLength(128);
        entity.Property(e => e.VpnServerNameSnapshot).HasMaxLength(128);
        entity.Property(e => e.ErrorMessage).HasMaxLength(1024);
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.NotificationChannel).HasMaxLength(32);
        entity.Property(e => e.NotificationSent).IsRequired();

        entity.HasIndex(e => e.VpnServerId);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.CreatedAt);
    }
}
