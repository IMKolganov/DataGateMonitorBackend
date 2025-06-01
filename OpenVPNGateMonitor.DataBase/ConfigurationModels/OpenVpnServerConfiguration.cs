using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.DataBase.ConfigurationModels.Seeds;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerConfiguration : BaseEntityConfiguration<OpenVpnServer>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServer> entity)
    {
        base.Configure(entity);
        entity.Property(e => e.ServerName)
            .IsRequired();
        entity.Property(e => e.IsOnline);
        entity.Property(e => e.IsDefault);
        entity.Property(e => e.ApiUrl)
            .HasMaxLength(255);
        
        entity.HasData(OpenVpnServerSeedData.Data);
    }
}