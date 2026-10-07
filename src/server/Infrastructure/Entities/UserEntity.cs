namespace Infrastructure.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
}
