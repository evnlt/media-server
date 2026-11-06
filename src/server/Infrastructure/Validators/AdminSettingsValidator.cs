using Infrastructure.Settings;

namespace Infrastructure.Validators;

public static class AdminSettingsValidator
{
    public static List<string> Validate(AdminSettings settings)
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(settings.Username) || settings.Username.Length > AdminSettings.MaxUsernameLength)
        {
            errors.Add($"Admin:Username must be set (1-{AdminSettings.MaxUsernameLength} characters).");
        }

        if (string.IsNullOrEmpty(settings.Password) ||
            settings.Password.Length < AdminSettings.MinPasswordLength ||
            settings.Password.Length > AdminSettings.MaxPasswordLength)
        {
            errors.Add(
                $"Admin:Password must be set ({AdminSettings.MinPasswordLength}-{AdminSettings.MaxPasswordLength} characters).");
        }

        return errors;
    }
}
