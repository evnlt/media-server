using Infrastructure.Entities;

namespace Application.Abstraction.Services;

public interface IAuthService
{
    Task<UserEntity?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
}
