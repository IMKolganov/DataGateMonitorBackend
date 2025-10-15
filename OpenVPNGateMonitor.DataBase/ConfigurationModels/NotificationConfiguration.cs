using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class NotificationConfiguration : BaseEntityConfiguration<Notification, int>
{
    public override void Configure(EntityTypeBuilder<Notification> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.Type)
            .HasMaxLength(128)
            .IsRequired();

        entity.Property(e => e.Severity)
            .IsRequired();

        entity.Property(e => e.Title)
            .IsRequired();

        entity.Property(e => e.Message)
            .IsRequired();

        entity.Property(e => e.Source)
            .HasMaxLength(64)
            .IsRequired();

        entity.Property(e => e.CorrelationId);
        entity.Property(e => e.DedupKey);
        entity.Property(e => e.ServerId);
        entity.Property(e => e.ActorUserId)
            .IsRequired();
        entity.Property(e => e.RelatedClientId);
        entity.Property(e => e.IsArchived)
            .IsRequired();

        // Indexes
        entity.HasIndex(e => e.Type)
            .HasDatabaseName("IX_Notification_Type");

        entity.HasIndex(e => e.Severity)
            .HasDatabaseName("IX_Notification_Severity");

        entity.HasIndex(e => e.ServerId)
            .HasDatabaseName("IX_Notification_ServerId");

        entity.HasIndex(e => e.ActorUserId)
            .HasDatabaseName("IX_Notification_ActorUserId");

        entity.HasIndex(e => new { e.Type, e.ServerId, e.DedupKey })
            .HasDatabaseName("IX_Notification_Type_ServerId_DedupKey");
    }
}