using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class IssuedOvpnFileTokenConfiguration : BaseEntityConfiguration<IssuedOvpnFileToken, int>
{
    public override void Configure(EntityTypeBuilder<IssuedOvpnFileToken> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(e => e.CreatedAt)
            .IsRequired();

        entity.Property(e => e.ExpiresAt)
            .IsRequired(false);

        entity.Property(e => e.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(e => e.Purpose)
            .HasMaxLength(255);

        entity.HasOne(e => e.IssuedOvpnFile)
            .WithMany()
            .HasForeignKey(e => e.IssuedOvpnFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}