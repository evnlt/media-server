using Infrastructure.Settings;

namespace Infrastructure.Validators;

public static class ResourceSettingsValidator
{
    private const int MinCpuPercentage = 1;
    private const int MaxCpuPercentage = 100;

    public static List<string> Validate(ResourceSettings settings)
    {
        List<string> errors = new();

        if (settings.CpuPercentage is < MinCpuPercentage or > MaxCpuPercentage)
        {
            errors.Add($"Resources:CpuPercentage must be between {MinCpuPercentage} and {MaxCpuPercentage}.");
        }

        return errors;
    }
}
