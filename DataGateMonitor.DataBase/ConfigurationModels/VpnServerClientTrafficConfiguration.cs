using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnServerClientTrafficConfiguration : BaseEntityConfiguration<VpnServerClientTraffic, int>
{
    public override void Configure(EntityTypeBuilder<VpnServerClientTraffic> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.VpnServerId).IsRequired();
        entity.Property(e => e.ExternalId).HasMaxLength(255);
        entity.Property(e => e.UserId);
        entity.Property(e => e.SessionId).IsRequired();
        entity.Property(e => e.BytesReceived).IsRequired();
        entity.Property(e => e.BytesSent).IsRequired();
        entity.Property(e => e.MeasuredAt).IsRequired();

        // Uniqueness (logical constraint)
        entity.HasIndex(e => new { e.VpnServerId, e.MeasuredAt, e.SessionId })
            .IsUnique()
            .HasDatabaseName("UX_ClientTraffic_Server_Session_At");

        // Existing read helpers
        entity.HasIndex(e => new { e.VpnServerId, e.MeasuredAt })
            .HasDatabaseName("IX_ClientTraffic_Server_At");

        entity.HasIndex(e => new { e.ExternalId, e.MeasuredAt })
            .HasDatabaseName("IX_ClientTraffic_External_At");
        
        // WHERE MeasuredAt BETWEEN ...
        entity.HasIndex(e => e.MeasuredAt)
            .HasDatabaseName("IX_ClientTraffic_At");

        // ORDER BY SessionId, MeasuredAt (plus WHERE by MeasuredAt)
        entity.HasIndex(e => new { e.MeasuredAt, e.SessionId })
            .HasDatabaseName("IX_ClientTraffic_At_Session");

        // ORDER BY ExternalId, SessionId, MeasuredAt (plus WHERE by MeasuredAt)
        entity.HasIndex(e => new { e.MeasuredAt, e.ExternalId, e.SessionId })
            .HasDatabaseName("IX_ClientTraffic_At_External_Session");
    }
}