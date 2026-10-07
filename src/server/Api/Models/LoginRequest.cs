using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public sealed class LoginRequest
{
    [Required] public string Username { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
}
