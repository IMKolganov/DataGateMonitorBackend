using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class UserIdentityLinkConfiguration : BaseEntityConfiguration<UserIdentityLink, int>
{
    public override void Configure(EntityTypeBuilder<UserIdentityLink> entity)
    {
        base.Configure(entity);

        entity.ToTable("UserIdentityLinks");

        // Logical reference only (no FK)
        entity.Property(e => e.UserId)
            .IsRequired();

        entity.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.ProviderRowId);

        // Indexes
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.Provider, e.ExternalId }).IsUnique();
    }
}