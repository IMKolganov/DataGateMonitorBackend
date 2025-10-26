using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;


public class UserQuotaPlanConfiguration : BaseEntityConfiguration<UserQuotaPlan, int>
{
    public override void Configure(EntityTypeBuilder<UserQuotaPlan> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.QuotaPlanId).IsRequired();

        entity.Property(e => e.Note).HasMaxLength(256);

        // One active plan per user (EffectiveTo == null)
        entity.HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("effective_to IS NULL");

        entity.HasIndex(e => e.EffectiveFrom);
    }
}