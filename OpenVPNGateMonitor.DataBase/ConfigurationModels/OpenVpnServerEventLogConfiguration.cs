using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerEventLogConfiguration : BaseEntityConfiguration<OpenVpnServerEventLog, int>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServerEventLog> entity)
    {
        base.Configure(entity);

        // Required columns
        entity.Property(e => e.VpnServerId).IsRequired();

        entity.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        // Optional strings with sane max lengths
        entity.Property(e => e.CommonName).HasMaxLength(255);
        entity.Property(e => e.RealAddress).HasMaxLength(255);     // "ip:port"
        entity.Property(e => e.VirtualAddress).HasMaxLength(255);
        entity.Property(e => e.ScriptType).HasMaxLength(64);
        entity.Property(e => e.Action).HasMaxLength(32);

        entity.Property(e => e.IvVer).HasMaxLength(64);
        entity.Property(e => e.IvGuiVer).HasMaxLength(128);
        entity.Property(e => e.IvPlat).HasMaxLength(64);

        // Timestamps
        entity.Property(e => e.ConnectedSince); // nullable
        entity.Property(e => e.EventTimeUtc)
              .IsRequired()
              .HasDefaultValueSql("now()"); // PostgreSQL timestamptz now()

        entity.Property(e => e.DisconnectedAt); // nullable

        // Big numeric traffic counters (PostgreSQL maps long -> bigint)
        entity.Property(e => e.BytesReceived);
        entity.Property(e => e.BytesSent);
        entity.Property(e => e.SampleBytesIn);
        entity.Property(e => e.SampleBytesOut);

        // Duration (sec)
        entity.Property(e => e.DurationSec);

        // Message as TEXT
        entity.Property(e => e.Message).HasColumnType("text");

        // ---------- Indexes ----------
        // Most used timeline queries per server
        entity.HasIndex(e => new { e.VpnServerId, e.EventTimeUtc })
              .HasDatabaseName("ix_ovpn_events_server_time");

        // Filter by server + type + time
        entity.HasIndex(e => new { e.VpnServerId, e.EventType, e.EventTimeUtc })
              .HasDatabaseName("ix_ovpn_events_server_type_time");

        // Filter by server + CN + time
        entity.HasIndex(e => new { e.VpnServerId, e.CommonName, e.EventTimeUtc })
              .HasDatabaseName("ix_ovpn_events_server_cn_time");
    }
}
