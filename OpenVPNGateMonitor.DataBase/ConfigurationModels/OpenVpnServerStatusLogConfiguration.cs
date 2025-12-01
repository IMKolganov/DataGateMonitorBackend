using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerStatusLogConfiguration : BaseEntityConfiguration<OpenVpnServerStatusLog, int>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServerStatusLog> entity)
    {
        base.Configure(entity);
        entity.Property(e => e.VpnServerId)
            .IsRequired();
        entity.Property(e => e.SessionId)
            .IsRequired();
        entity.Property(e => e.UpSince)
            .IsRequired();
        entity.Property(e => e.ServerLocalIp)
            .HasMaxLength(255);
        entity.Property(e => e.ServerRemoteIp)
            .HasMaxLength(255);
        entity.Property(e => e.BytesIn)
            .IsRequired();
        entity.Property(e => e.BytesOut)
            .IsRequired();
        entity.Property(e => e.Version)
            .IsRequired();
        
        // Index for aggregations by server (SUM(BytesIn/BytesOut) WHERE VpnServerId = ...)
        entity.HasIndex(e => e.VpnServerId)
            .HasDatabaseName("IX_ServerStatusLogs_Server");

        // Index for ROW_NUMBER() OVER (PARTITION BY VpnServerId ORDER BY Id DESC)
        entity.HasIndex(e => new { e.VpnServerId, e.Id })
            .HasDatabaseName("IX_ServerStatusLogs_Server_Id");

        // Optional: queries that hit by server + session
        entity.HasIndex(e => new { e.VpnServerId, e.SessionId })
            .HasDatabaseName("IX_ServerStatusLogs_Server_Session");
    }
}