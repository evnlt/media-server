using Application.Abstraction.Services;
using Infrastructure.Auth;
using Infrastructure.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<UserEntity> _hasher;

    public AuthService(AppDbContext db, IPasswordHasher<UserEntity> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<UserEntity?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (username.Length > AdminOptions.MaxUsernameLength || password.Length > AdminOptions.MaxPasswordLength)
        {
            return null;
        }

        UserEntity? user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (user is null || !string.Equals(user.Username, username))
        {
            return null;
        }

        PasswordVerificationResult result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result != PasswordVerificationResult.Failed ? user : null;
    }
}
