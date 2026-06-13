using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class MergedUserArchiveConfiguration : BaseEntityConfiguration<MergedUserArchive, int>
{
    public override void Configure(EntityTypeBuilder<MergedUserArchive> entity)
    {
        base.Configure(entity);

        entity.ToTable("MergedUserArchives");

        entity.Property(e => e.OriginalUserId).IsRequired();
        entity.Property(e => e.MergedIntoUserId).IsRequired();
        entity.Property(e => e.MergedAt).IsRequired();

        entity.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.Email)
            .HasMaxLength(256);

        entity.Property(e => e.AvatarUrl)
            .HasMaxLength(2048);

        entity.Property(e => e.IdentityLinksJson)
            .IsRequired()
            .HasColumnType("jsonb");

        entity.Property(e => e.MergeReportJson)
            .IsRequired()
            .HasColumnType("jsonb");

        entity.Property(e => e.Note)
            .HasMaxLength(512);

        entity.HasIndex(e => e.OriginalUserId);
        entity.HasIndex(e => e.MergedIntoUserId);
        entity.HasIndex(e => e.MergedAt);
    }
}
