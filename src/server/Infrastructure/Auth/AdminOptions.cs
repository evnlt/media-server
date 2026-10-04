namespace Infrastructure.Auth;

public class AdminOptions
{
    public const string SectionName = "Admin";
    public const int MinPasswordLength = 10;
    public const int MaxPasswordLength = 256;
    public const int MaxUsernameLength = 100;

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
