using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public sealed class UserRefreshTokenConfiguration
    : BaseEntityConfiguration<UserRefreshToken, int>
{
    public override void Configure(EntityTypeBuilder<UserRefreshToken> entity)
    {
        base.Configure(entity);

        entity.ToTable("UserRefreshTokens");

        entity.Property(e => e.UserId)
            .IsRequired();

        entity.Property(e => e.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.CreatedAt)
            .IsRequired();

        entity.Property(e => e.ExpiresAt)
            .IsRequired();

        entity.Property(e => e.RevokedAt);

        entity.Property(e => e.ReplacedByTokenId);

        entity.Property(e => e.DeviceId)
            .HasMaxLength(128);

        entity.Property(e => e.UserAgent)
            .HasMaxLength(256);

        // Indexes
        entity.HasIndex(e => e.TokenHash)
            .IsUnique();

        entity.HasIndex(e => new { e.UserId, e.DeviceId });

        entity.HasIndex(e => new { e.TokenHash, e.RevokedAt, e.ExpiresAt });
        
        entity.HasIndex(e => e.ExpiresAt);

        entity.HasIndex(e => e.RevokedAt);
    }
}