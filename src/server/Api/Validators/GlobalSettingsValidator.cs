using Api.Settings;
using Infrastructure.Validators;

namespace Api.Validators;

public static class GlobalSettingsValidator
{
    public static void EnsureValid(GlobalSettings settings)
    {
        List<string> errors = new();

        Validate("Admin", settings.Admin, AdminSettingsValidator.Validate, errors);
        Validate("Storage", settings.Storage, StorageSettingsValidator.Validate, errors);
        Validate("Resources", settings.Resources, ResourceSettingsValidator.Validate, errors);
        Validate("Transcode", settings.Transcode, TranscodeSettingsValidator.Validate, errors);

        if (errors.Count == 0)
        {
            return;
        }

        string details = string.Join(Environment.NewLine, errors.Select(error => $"  - {error}"));
        throw new InvalidOperationException(
            $"Invalid configuration:{Environment.NewLine}{details}{Environment.NewLine}" +
            "Set them in appsettings.json, user secrets or environment variables, e.g. Admin__Username or Transcode__Codec=Av1 (see .env.example).");
    }

    private static void Validate<T>(string section, T? settings, Func<T, List<string>> validate, List<string> errors)
        where T : class
    {
        if (settings is null)
        {
            errors.Add($"{section} section is missing.");
            return;
        }

        errors.AddRange(validate(settings));
    }
}
