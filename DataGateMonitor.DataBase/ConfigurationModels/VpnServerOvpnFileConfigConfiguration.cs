using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.DataBase.ConfigurationModels.Seeds;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnServerOvpnFileConfigConfiguration : BaseEntityConfiguration<VpnServerOvpnFileConfig, int>
{
    public override void Configure(EntityTypeBuilder<VpnServerOvpnFileConfig> entity)
    {
        base.Configure(entity);
        entity.Property(e => e.VpnServerId)
            .IsRequired();
        entity.Property(e => e.VpnServerIp)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.VpnServerPort)
            .IsRequired();
        entity.Property(e => e.ConfigTemplate)
            .IsRequired()
            .HasColumnType("TEXT");
        
        entity.HasData(VpnServerOvpnFileConfigSeedData.Data);
    }
}