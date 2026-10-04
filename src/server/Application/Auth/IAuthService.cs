using Application.Entities;

namespace Application.Auth;

public interface IAuthService
{
    Task<User?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}
