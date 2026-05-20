using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class WindowsCrashReportConfiguration : BaseEntityConfiguration<WindowsCrashReport, long>
{
    public override void Configure(EntityTypeBuilder<WindowsCrashReport> entity)
    {
        base.Configure(entity);

        entity.ToTable("WindowsCrashReports");

        entity.Property(e => e.AppProcess).IsRequired().HasMaxLength(256);
        entity.Property(e => e.FileName).IsRequired().HasMaxLength(512);
        entity.Property(e => e.PayloadRaw).IsRequired().HasColumnType("text");
        entity.Property(e => e.ParseStatus).IsRequired().HasMaxLength(32);

        entity.Property(e => e.Process).HasMaxLength(256);
        entity.Property(e => e.Thread).HasMaxLength(256);
        entity.Property(e => e.Sdk).HasMaxLength(128);
        entity.Property(e => e.Device).HasMaxLength(256);
        entity.Property(e => e.Kind).HasMaxLength(32);
        entity.Property(e => e.Exception).HasMaxLength(512);
        entity.Property(e => e.Message).HasMaxLength(4000);
        entity.Property(e => e.Tag).HasMaxLength(256);
        entity.Property(e => e.Stacktrace).HasColumnType("text");

        entity.HasIndex(e => e.CreateDate);
        entity.HasIndex(e => new { e.AppProcess, e.CreateDate });
        entity.HasIndex(e => e.ParseStatus);
    }
}
