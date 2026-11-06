namespace Api.Models;

public record CurrentUserResponse
{
    public required string Username { get; init; }
}
