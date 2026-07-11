using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class UserPasswordHistoryConfiguration : BaseEntityConfiguration<UserPasswordHistory, int>
{
    public override void Configure(EntityTypeBuilder<UserPasswordHistory> entity)
    {
        base.Configure(entity);

        entity.ToTable("UserPasswordHistory");

        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.UserCredentialId).IsRequired();
        entity.Property(e => e.PasswordHash).IsRequired();
        entity.Property(e => e.PasswordAlgo).IsRequired().HasMaxLength(32);
        entity.Property(e => e.RecordedAtUtc).IsRequired();
        entity.Property(e => e.SetByActor).IsRequired();
        entity.Property(e => e.Reason).HasMaxLength(256);

        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserId, e.RecordedAtUtc });
    }
}
