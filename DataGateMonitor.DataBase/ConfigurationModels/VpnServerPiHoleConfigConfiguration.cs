using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnServerPiHoleConfigConfiguration : BaseEntityConfiguration<VpnServerPiHoleConfig, int>
{
    public override void Configure(EntityTypeBuilder<VpnServerPiHoleConfig> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.VpnServerId).IsRequired();
        entity.Property(e => e.BaseUrl).HasMaxLength(512).IsRequired();
        entity.Property(e => e.AppPassword).HasMaxLength(512);
        entity.Property(e => e.ClientSubnetPrefix).HasMaxLength(64);

        entity.HasIndex(e => e.VpnServerId)
            .IsUnique()
            .HasDatabaseName("ux_vpn_server_pihole_config_server_id");
    }
}
