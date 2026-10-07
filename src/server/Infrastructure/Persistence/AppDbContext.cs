using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<MediaFileEntity> MediaFiles => Set<MediaFileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<MediaFileEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(500);
            e.Property(x => x.StoragePath).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Status);
        });
    }
}
