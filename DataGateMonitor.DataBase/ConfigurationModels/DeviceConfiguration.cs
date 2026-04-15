using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class DeviceConfiguration : BaseEntityConfiguration<Device, int>
{
    public override void Configure(EntityTypeBuilder<Device> entity)
    {
        base.Configure(entity);

        entity.ToTable("Devices");
        
        entity.Property(e => e.UserId)
            .IsRequired();

        entity.Property(e => e.InstallationId)
            .IsRequired()
            .HasMaxLength(128);

        // Indexes
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.InstallationId);
        entity.HasIndex(e => new { e.UserId, e.InstallationId })
            .HasDatabaseName("IX_Devices_User_InstallationId");
    }
}