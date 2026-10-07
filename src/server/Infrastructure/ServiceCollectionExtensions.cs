using Infrastructure.Auth;
using Infrastructure.Entities;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        EnsureDatabaseDirectoryExists(connectionString);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

        services.AddOptions<AdminOptions>().BindConfiguration(AdminOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminOptions>, AdminOptionsValidator>();
        services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();

        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddSingleton<IStorageProvider, LocalStorageProvider>();

        return services;
    }

    public static void MigrateDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }

    private static void EnsureDatabaseDirectoryExists(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
