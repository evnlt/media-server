using Infrastructure.Entities;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;

        AdminSettings settings = serviceProvider.GetRequiredService<AdminSettings>();
        AppDbContext db = serviceProvider.GetRequiredService<AppDbContext>();
        IPasswordHasher<UserEntity> hasher = serviceProvider.GetRequiredService<IPasswordHasher<UserEntity>>();
        ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

        List<UserEntity> users = await db.Users.ToListAsync(cancellationToken);
        if (users.Count > 1)
        {
            throw new InvalidOperationException($"Expected exactly one user but found {users.Count}.");
        }

        UserEntity? user = users.SingleOrDefault();
        if (user is null)
        {
            user = new UserEntity { Id = Guid.NewGuid(), Username = settings.Username, PasswordHash = "" };
            user.PasswordHash = hasher.HashPassword(user, settings.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created admin user '{Username}'.", user.Username);
            return;
        }

        bool userChanged = false;

        if (user.Username != settings.Username)
        {
            user.Username = settings.Username;
            userChanged = true;
        }

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, settings.Password) != PasswordVerificationResult.Success)
        {
            user.PasswordHash = hasher.HashPassword(user, settings.Password);
            userChanged = true;
        }

        if (userChanged)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated admin credentials for '{Username}' from configuration.", user.Username);
        }
    }
}
