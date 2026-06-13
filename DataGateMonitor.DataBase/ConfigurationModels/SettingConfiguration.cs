using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.DataBase.ConfigurationModels.Seeds;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class SettingConfiguration : BaseEntityConfiguration<Setting, int>
{
    public override void Configure(EntityTypeBuilder<Setting> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.ValueType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.StringValue)
            .HasMaxLength(255);

        entity.Property(e => e.IntValue);
        entity.Property(e => e.BoolValue);
        entity.Property(e => e.DoubleValue);
        entity.Property(e => e.DateTimeValue);
        
        entity.HasData(SettingSeedData.Data);
    }
}