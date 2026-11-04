using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataGateMonitor.DataBase.ConfigurationModels;

public class IssuedOvpnFileConfiguration : BaseEntityConfiguration<IssuedOvpnFile, int>
{
    public override void Configure(EntityTypeBuilder<IssuedOvpnFile> entity)
    {
        base.Configure(entity);
        entity.Property(e => e.VpnServerId)
            .IsRequired();
        entity.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.CommonName)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.CertId)
            .HasMaxLength(255);
        entity.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(500);
        entity.Property(e => e.IssuedAt)
            .IsRequired();
        entity.Property(e => e.IssuedTo)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.PemFilePath)
            .IsRequired()
            .HasMaxLength(500);
        entity.Property(e => e.CertFilePath)
            .IsRequired()
            .HasMaxLength(500);
        entity.Property(e => e.KeyFilePath)
            .IsRequired()
            .HasMaxLength(500);
        entity.Property(e => e.ReqFilePath)
            .IsRequired()
            .HasMaxLength(500);
        entity.Property(e => e.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);
        entity.Property(e => e.Message)
            .HasMaxLength(500);
        entity.Property(e => e.Origin)
            .IsRequired()
            .HasDefaultValue(IssuedOvpnFileOrigin.Dashboard);
        entity.Property(e => e.IsServiceProfile)
            .IsRequired()
            .HasDefaultValue(false);
        entity.Property(e => e.CertSerialNumber)
            .HasMaxLength(128);
        entity.Property(e => e.CertFingerprintSha256)
            .HasMaxLength(64);

        // Node events are idempotent per certificate serial.
        entity.HasIndex(e => new { e.VpnServerId, e.CertSerialNumber })
            .IsUnique()
            .HasDatabaseName("IX_IssuedOvpnFiles_VpnServerId_CertSerialNumber")
            .HasFilter("\"CertSerialNumber\" IS NOT NULL");
    }
}