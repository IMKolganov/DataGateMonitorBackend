using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class IssuedXrayClientLinkTokenConfiguration : BaseEntityConfiguration<IssuedXrayClientLinkToken, int>
{
    public override void Configure(EntityTypeBuilder<IssuedXrayClientLinkToken> entity)
    {
        base.Configure(entity);
        entity.Property(e => e.IssuedXrayClientLinkId).IsRequired();
        entity.Property(e => e.Token).IsRequired().HasMaxLength(512);
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.IsUsed).IsRequired();
        entity.HasOne(e => e.IssuedXrayClientLink)
            .WithMany()
            .HasForeignKey(e => e.IssuedXrayClientLinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
