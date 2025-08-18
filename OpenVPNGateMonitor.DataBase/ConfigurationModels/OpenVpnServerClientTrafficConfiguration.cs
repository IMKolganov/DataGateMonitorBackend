using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerClientTrafficConfiguration : BaseEntityConfiguration<OpenVpnServerClientTraffic, int>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServerClientTraffic> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.VpnServerId).IsRequired();
        entity.Property(e => e.ExternalId).HasMaxLength(255);
        entity.Property(e => e.SessionId).IsRequired();
        entity.Property(e => e.BytesReceived).IsRequired();
        entity.Property(e => e.BytesSent).IsRequired();
        entity.Property(e => e.MeasuredAt).IsRequired();

        // Unique per server+session+timestamp
        entity.HasIndex(e => new { e.VpnServerId, e.SessionId, e.MeasuredAt })
            .IsUnique()
            .HasDatabaseName("UX_ClientTraffic_Server_Session_At");

        // Helpful read indexes
        entity.HasIndex(e => new { e.VpnServerId, e.MeasuredAt })
            .HasDatabaseName("IX_ClientTraffic_Server_At");

        entity.HasIndex(e => new { e.ExternalId, e.MeasuredAt })
            .HasDatabaseName("IX_ClientTraffic_External_At");
    }
}