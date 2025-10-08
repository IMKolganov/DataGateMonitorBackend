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
        entity.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_notifications_created_at");
        entity.HasIndex(e => e.Type)
            .HasDatabaseName("ix_notifications_type");
        entity.HasIndex(e => e.Severity)
            .HasDatabaseName("ix_notifications_severity");
        entity.HasIndex(e => e.ServerId)
            .HasDatabaseName("ix_notifications_server");
        entity.HasIndex(e => e.ActorUserId)
            .HasDatabaseName("ix_notifications_actor");
        entity.HasIndex(e => new { e.Type, e.ServerId, e.DedupKey })
            .HasDatabaseName("ix_notifications_dedup");
    }
}