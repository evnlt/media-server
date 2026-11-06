using System.Threading.RateLimiting;
using Api.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Extensions;

public static class AuthenticationExtensions
{
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services,
        DataProtectionSettings dataProtection)
    {
        if (string.IsNullOrWhiteSpace(dataProtection?.KeysPath))
        {
            throw new InvalidOperationException("DataProtection:KeysPath is not configured.");
        }

        // Persisted so sessions survive container restarts and redeploys.
        services.AddDataProtection()
            .SetApplicationName("media-server")
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtection.KeysPath));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.Name = "media-server.auth";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.ExpireTimeSpan = TimeSpan.FromDays(30);
                o.SlidingExpiration = true;
                o.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                o.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        // Deny by default: anything not explicitly [AllowAnonymous] needs a session.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(LoginRateLimitPolicy, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                    }));
        });

        return services;
    }
}
