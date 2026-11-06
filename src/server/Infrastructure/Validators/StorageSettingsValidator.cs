using Infrastructure.Settings;

namespace Infrastructure.Validators;

public static class StorageSettingsValidator
{
    public static List<string> Validate(StorageSettings settings)
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(settings.RootPath))
        {
            errors.Add("Storage:RootPath must be set.");
        }

        return errors;
    }
}
