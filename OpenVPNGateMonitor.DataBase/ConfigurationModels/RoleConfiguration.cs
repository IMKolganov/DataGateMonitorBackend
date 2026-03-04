using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.DataBase.ConfigurationModels.Seeds;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.Description)
            .HasMaxLength(256);

        entity.Property(e => e.IsSystem)
            .IsRequired();

        // Unique constraint for role names (case-insensitive search via NormalizedName)
        entity.HasIndex(e => e.NormalizedName)
            .IsUnique();

        // Optional: just for convenience when browsing roles
        entity.HasIndex(e => e.Name);
        
        entity.HasData(RoleSeedData.Data);
    }
}
