using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public sealed record LoginRequest
{
    [Required] public required string Username { get; init; }
    [Required] public required string Password { get; init; }
}
