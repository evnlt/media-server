using System.Security.Claims;
using System.Threading.RateLimiting;
using Application.Auth;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Auth;

public static class AuthExtensions
{
    private const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Persisted so sessions survive container restarts and redeploys.
        var keysPath = configuration["DataProtection:KeysPath"] ?? "data/keys";
        services.AddDataProtection()
            .SetApplicationName("media-server")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

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
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth, HttpContext http, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.Username)
                    || string.IsNullOrEmpty(request.Password)
                    || request.Username.Length > AdminOptions.MaxUsernameLength
                    || request.Password.Length > AdminOptions.MaxPasswordLength)
                    return Results.BadRequest();

                var user = await auth.ValidateCredentialsAsync(request.Username, request.Password, ct);
                if (user is null)
                    return Results.Unauthorized();

                var identity = new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Username)],
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await http.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true });

                return Results.NoContent();
            })
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimitPolicy);

        app.MapPost("/api/auth/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new { username = user.Identity?.Name }));

        return app;
    }

    private record LoginRequest(string? Username, string? Password);
}
