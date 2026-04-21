using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeilleBoisee.Domain.Entities;

namespace VeilleBoisee.Infrastructure.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CommuneInsee).HasMaxLength(10).IsRequired();
        builder.Property(r => r.CommuneName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(Report.DescriptionMaxLength).IsRequired();
        // Encrypted email: IV (16 bytes) + ciphertext (≤320 bytes), base64-encoded ≈ 450 chars
        builder.Property(r => r.EncryptedContactEmail).HasMaxLength(600).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.SubmittedAt).IsRequired();

        builder.Property(r => r.ParcelleSection).HasMaxLength(10);
        builder.Property(r => r.ParcelleNumero).HasMaxLength(10);

        builder.HasIndex(r => r.CommuneInsee);
        builder.HasIndex(r => r.SubmittedAt);

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
