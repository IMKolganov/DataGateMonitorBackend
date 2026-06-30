using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnDnsQueryLogConfiguration : BaseEntityConfiguration<VpnDnsQueryLog, int>
{
    public override void Configure(EntityTypeBuilder<VpnDnsQueryLog> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.VpnServerId).IsRequired();
        entity.Property(e => e.PiHoleQueryId).IsRequired();
        entity.Property(e => e.ClientIp).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Domain).HasMaxLength(512).IsRequired();
        entity.Property(e => e.CommonName).HasMaxLength(255);
        entity.Property(e => e.ExternalId).HasMaxLength(128);
        entity.Property(e => e.QueryType).HasMaxLength(32);
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.QueriedAtUtc).IsRequired();

        entity.HasIndex(e => new { e.VpnServerId, e.PiHoleQueryId })
            .IsUnique()
            .HasDatabaseName("ux_vpn_dns_query_server_pihole_id");

        entity.HasIndex(e => new { e.VpnServerId, e.QueriedAtUtc })
            .HasDatabaseName("ix_vpn_dns_query_server_time");

        entity.HasIndex(e => new { e.VpnServerId, e.ExternalId, e.QueriedAtUtc })
            .HasDatabaseName("ix_vpn_dns_query_server_external_time");

        entity.HasIndex(e => new { e.VpnServerId, e.Domain, e.QueriedAtUtc })
            .HasDatabaseName("ix_vpn_dns_query_server_domain_time");
    }
}
