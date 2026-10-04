using Application.Auth;
using Application.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Auth;

public class AuthService(AppDbContext db, IPasswordHasher<User> hasher) : IAuthService
{
    private static readonly User DummyUser = new() { Username = "", PasswordHash = "" };
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(DummyUser, "dummy-password-for-timing");

    public async Task<User?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var usernameMatches = user is not null && string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase);

        // Always verify a hash so a wrong username takes as long as a wrong password.
        var result = hasher.VerifyHashedPassword(user ?? DummyUser, usernameMatches ? user!.PasswordHash : DummyHash, password);

        return usernameMatches && result != PasswordVerificationResult.Failed ? user : null;
    }
}
