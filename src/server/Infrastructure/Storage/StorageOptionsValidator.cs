using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        return string.IsNullOrWhiteSpace(options.RootPath)
            ? ValidateOptionsResult.Fail("Storage:RootPath must be set (see Storage__RootPath in .env.example).")
            : ValidateOptionsResult.Success;
    }
}
