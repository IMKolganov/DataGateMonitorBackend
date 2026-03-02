using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.DataBase.ConfigurationModels.Seeds;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerConfiguration : BaseEntityConfiguration<OpenVpnServer, int>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServer> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.ServerName)
            .IsRequired();

        entity.Property(e => e.IsOnline);
        entity.Property(e => e.IsDefault);
        entity.Property(e => e.IsDisable);

        entity.Property(e => e.ApiUrl)
            .HasMaxLength(255);

        entity.Property(e => e.Latitude)
            .HasPrecision(9, 6); // Up to ~10cm accuracy

        entity.Property(e => e.Longitude)
            .HasPrecision(9, 6);
        
        entity.Property(e => e.IsEnableWss);
        entity.Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        // Indexes
        entity.HasIndex(e => e.ServerName)
            .IsUnique();

        entity.HasIndex(e => e.IsOnline);

        entity.HasIndex(e => e.IsDefault);

        entity.HasIndex(e => e.IsDisable);
        
        entity.HasIndex(e => e.IsEnableWss);

        entity.HasIndex(e => new { e.Latitude, e.Longitude });
        entity.HasIndex(e => e.IsDeleted);

        // Seed data
        entity.HasData(OpenVpnServerSeedData.Data);
    }
}