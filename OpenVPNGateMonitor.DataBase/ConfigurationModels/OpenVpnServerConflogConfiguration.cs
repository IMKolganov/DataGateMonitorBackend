using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerConflogConfiguration : BaseEntityConfiguration<OpenVpnServerConflog, int>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServerConflog> entity)
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
