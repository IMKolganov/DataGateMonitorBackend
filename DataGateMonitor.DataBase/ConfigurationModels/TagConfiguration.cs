using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class TagConfiguration : BaseEntityConfiguration<Tag, int>
{
    public override void Configure(EntityTypeBuilder<Tag> entity)
    {
        base.Configure(entity);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(64);

        entity.HasIndex(e => e.Name)
            .IsUnique();
    }
}
