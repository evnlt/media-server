using Infrastructure.Settings;

namespace Api.Settings;

public class GlobalSettings
{
    public ConnectionStringSettings ConnectionStrings { get; set; } = default!;

    public DataProtectionSettings DataProtection { get; set; } = default!;

    public AdminSettings Admin { get; set; } = default!;

    public StorageSettings Storage { get; set; } = default!;

    public ResourceSettings Resources { get; set; } = default!;

    public TranscodeSettings Transcode { get; set; } = default!;
}
