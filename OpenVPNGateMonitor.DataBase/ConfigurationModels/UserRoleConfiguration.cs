using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
        entity.Property(e => e.UserId)
            .IsRequired();

        entity.Property(e => e.RoleId)
            .IsRequired();

        // Composite primary key
        entity.HasKey(e => new { e.UserId, e.RoleId });

        // Indexes for quick filtering
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.RoleId);

        // Foreign keys
        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<Role>()
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
