using Infrastructure.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var options = sp.GetRequiredService<IOptions<AdminOptions>>().Value;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher<UserEntity>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

        var users = await db.Users.ToListAsync(cancellationToken);
        if (users.Count > 1)
        {
            throw new InvalidOperationException($"Expected exactly one user but found {users.Count}.");
        }

        var user = users.SingleOrDefault();
        if (user is null)
        {
            user = new UserEntity { Id = Guid.NewGuid(), Username = options.Username, PasswordHash = "" };
            user.PasswordHash = hasher.HashPassword(user, options.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created admin user '{Username}'.", user.Username);
            return;
        }

        var changed = false;

        if (user.Username != options.Username)
        {
            user.Username = options.Username;
            changed = true;
        }

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, options.Password) != PasswordVerificationResult.Success)
        {
            user.PasswordHash = hasher.HashPassword(user, options.Password);
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated admin credentials for '{Username}' from configuration.", user.Username);
        }
    }
}
