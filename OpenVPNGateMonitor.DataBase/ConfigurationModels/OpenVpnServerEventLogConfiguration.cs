using OpenVPNGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels;

public class OpenVpnServerEventLogConfiguration : BaseEntityConfiguration<OpenVpnServerEventLog>
{
    public override void Configure(EntityTypeBuilder<OpenVpnServerEventLog> entity)
    {
        base.Configure(entity);
        
        entity.Property(e => e.VpnServerId)
            .IsRequired();

        entity.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(e => e.CommonName)
            .HasMaxLength(255);

        entity.Property(e => e.RealAddress)
            .HasMaxLength(255);

        entity.Property(e => e.VirtualAddress)
            .HasMaxLength(255);

        entity.Property(e => e.ConnectedSince);

        entity.Property(e => e.Message)
            .HasColumnType("text");

        entity.Property(e => e.RawJson)
            .HasColumnType("text")
            .IsRequired();
    }
}