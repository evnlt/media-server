using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

public class AdminOptionsValidator : IValidateOptions<AdminOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Username) || options.Username.Length > AdminOptions.MaxUsernameLength)
        {
            errors.Add($"Admin:Username must be set (1-{AdminOptions.MaxUsernameLength} characters).");
        }

        if (options.Password.Length < AdminOptions.MinPasswordLength ||
            options.Password.Length > AdminOptions.MaxPasswordLength)
        {
            errors.Add(
                $"Admin:Password must be set ({AdminOptions.MinPasswordLength}-{AdminOptions.MaxPasswordLength} characters).");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors.Append(
                "Set them via environment variables Admin__Username / Admin__Password (see .env.example)."));
    }
}