using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class VpnServerConflogConfiguration : BaseEntityConfiguration<VpnServerConflog, int>
{
    public override void Configure(EntityTypeBuilder<VpnServerConflog> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.VpnServerId);
        entity.Property(e => e.RequestUrl)
            .IsRequired()
            .HasMaxLength(512);
        entity.Property(e => e.PayloadJson)
            .IsRequired()
            .HasColumnType("text");

        entity.HasIndex(e => e.VpnServerId);
        entity.HasIndex(e => e.RequestUrl);
        entity.HasIndex(e => e.CreateDate);
    }
}
