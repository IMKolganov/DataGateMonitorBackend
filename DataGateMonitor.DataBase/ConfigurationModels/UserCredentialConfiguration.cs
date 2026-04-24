using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class UserCredentialConfiguration : BaseEntityConfiguration<UserCredential, int>
{
    public override void Configure(EntityTypeBuilder<UserCredential> entity)
    {
        base.Configure(entity);

        entity.ToTable("UserCredentials");

        // Logical reference only (no FK)
        entity.Property(e => e.UserId)
            .IsRequired();

        entity.Property(e => e.Login)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.NormalizedLogin)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.PasswordHash)
            .IsRequired();

        entity.Property(e => e.PasswordAlgo)
            .HasMaxLength(32)
            .HasDefaultValue("AspNetCoreV3");

        entity.Property(e => e.PasswordUpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(e => e.FailedCount)
            .IsRequired();

        entity.Property(e => e.LockoutUntilUtc);

        // Indexes
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.Login).IsUnique();
        entity.HasIndex(e => e.NormalizedLogin).IsUnique();
    }
}