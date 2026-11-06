using Infrastructure.Abstraction.Services;
using Infrastructure.Entities;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, ConnectionStringSettings connectionStrings)
    {
        if (string.IsNullOrWhiteSpace(connectionStrings?.Default))
        {
            throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        }

        string connectionString = connectionStrings.Default;

        EnsureDatabaseDirectoryExists(connectionString);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

        services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();
        services.AddSingleton<IStorageProvider, LocalStorageProvider>();
        services.AddSingleton<IVideoTranscoder, FfmpegVideoTranscoder>();

        return services;
    }

    public static Task EnsureTranscoderAvailableAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        return services.GetRequiredService<IVideoTranscoder>().EnsureAvailableAsync(cancellationToken);
    }

    public static int StopLeftoverTranscoders(this IServiceProvider services)
    {
        return services.GetRequiredService<IVideoTranscoder>().StopLeftovers();
    }

    public static void MigrateDatabase(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }

    private static void EnsureDatabaseDirectoryExists(string connectionString)
    {
        string dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        string? directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
