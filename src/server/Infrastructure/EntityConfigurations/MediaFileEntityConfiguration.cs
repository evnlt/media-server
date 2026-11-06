using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public sealed class MediaFileEntityConfiguration : IEntityTypeConfiguration<MediaFileEntity>
{
    public void Configure(EntityTypeBuilder<MediaFileEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.StoragePath)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(MediaFileEntity.FailureReasonMaxLength);

        builder.HasIndex(x => x.Status);
    }
}
