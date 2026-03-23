using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerClientConfiguration : BaseEntityConfiguration<OpenVpnServerClient, int>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServerClient> entity)
    {
        base.Configure(entity);
        entity.Property(e => e.VpnServerId)
            .IsRequired();
        entity.Property(e => e.ExternalId).HasMaxLength(255);
        entity.Property(e => e.UserId);
        entity.Property(e => e.SessionId)
            .IsRequired();
        entity.Property(e => e.CommonName)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.RemoteIp)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.ProxyRealIp)
            .HasMaxLength(128);
        entity.Property(e => e.LocalIp)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.BytesReceived)
            .IsRequired();
        entity.Property(e => e.BytesSent)
            .IsRequired();
        entity.Property(e => e.ConnectedSince)
            .IsRequired();
        entity.Property(e => e.DisconnectedAt);
        entity.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.Country)
            .HasMaxLength(100);
        entity.Property(e => e.Region)
            .HasMaxLength(100);
        entity.Property(e => e.City)
            .HasMaxLength(100);
        entity.Property(e => e.Latitude);
        entity.Property(e => e.Longitude);
        entity.Property(e => e.IsConnected);
        
        // Unique per server + session
        entity.HasIndex(e => new { e.VpnServerId, e.SessionId })
            .IsUnique()
            .HasDatabaseName("UX_OpenVpnServerClients_Server_Session");

        // Counts and filters by server + connection state
        entity.HasIndex(e => new { e.VpnServerId, e.IsConnected })
            .HasDatabaseName("IX_OpenVpnServerClients_Server_IsConnected");

        // Updates and checks by server + connection state + session
        entity.HasIndex(e => new { e.VpnServerId, e.IsConnected, e.SessionId })
            .HasDatabaseName("IX_OpenVpnServerClients_Server_IsConnected_Session");

        // Period-based queries and DISTINCT ExternalId per period
        entity.HasIndex(e => new { e.ConnectedSince, e.ExternalId })
            .HasDatabaseName("IX_OpenVpnServerClients_ConnectedSince_ExternalId");

        // Geo reports by period and coordinates
        entity.HasIndex(e => new { e.ConnectedSince, e.Latitude, e.Longitude })
            .HasDatabaseName("IX_OpenVpnServerClients_ConnectedSince_Lat_Lon");
    }
}