using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class UserConfiguration : BaseEntityConfiguration<User, int>
{
    public override void Configure(EntityTypeBuilder<User> entity)
    {
        base.Configure(entity);

        entity.ToTable("Users");

        entity.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.Email)
            .HasMaxLength(256);

        entity.Property(e => e.IsAdmin)
            .IsRequired();

        entity.Property(e => e.IsBlocked)
            .IsRequired();

        entity.Property(e => e.HasDashboardAccess)
            .IsRequired();

        // Indexes
        entity.HasIndex(e => e.Email);
        entity.HasIndex(e => e.HasDashboardAccess);
    }
}