using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.DataBase.ConfigurationModels.Seeds;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class LocalizationTextConfiguration : BaseEntityConfiguration<LocalizationText, int>
{
    public override void Configure(EntityTypeBuilder<LocalizationText> entity)
    {
        base.Configure(entity);
        
        entity.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.Language)
            .IsRequired()
            .HasConversion<int>();

        entity.Property(e => e.Text)
            .IsRequired();
        
        entity.HasData(LocalizationTextSeedData.GetData());
    }
}