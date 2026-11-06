namespace Infrastructure.Settings;

public class AdminSettings
{
    public const int MinPasswordLength = 10;
    public const int MaxPasswordLength = 256;
    public const int MaxUsernameLength = 100;

    public string Username { get; set; } = default!;

    public string Password { get; set; } = default!;
}
