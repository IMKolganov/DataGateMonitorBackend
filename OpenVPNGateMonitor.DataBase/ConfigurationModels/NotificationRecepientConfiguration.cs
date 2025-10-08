using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class NotificationRecipientConfiguration : BaseEntityConfiguration<NotificationRecipient, int>
{
    public override void Configure(EntityTypeBuilder<NotificationRecipient> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.NotificationId)
            .IsRequired();

        entity.Property(e => e.AdminUserId)
            .IsRequired();

        entity.Property(e => e.DeliveryChannel)
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(e => e.DeliveredAt);
        entity.Property(e => e.DeliveryStatus)
            .IsRequired();
        entity.Property(e => e.ReadAt);

        // Indexes
        entity.HasIndex(e => e.NotificationId)
            .HasDatabaseName("ix_notificationrecipients_notification");
        entity.HasIndex(e => e.AdminUserId)
            .HasDatabaseName("ix_notificationrecipients_admin");
        entity.HasIndex(e => new { e.AdminUserId, e.DeliveryStatus })
            .HasDatabaseName("ix_notificationrecipients_admin_status");
        entity.HasIndex(e => new { e.NotificationId, e.AdminUserId, e.DeliveryChannel })
            .IsUnique()
            .HasDatabaseName("ux_notificationrecipients_unique");
    }
}