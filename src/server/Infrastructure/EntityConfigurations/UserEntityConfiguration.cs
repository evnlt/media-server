using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Username)
            .IsUnique();

        builder.Property(x => x.PasswordHash)
            .IsRequired();
    }
}
