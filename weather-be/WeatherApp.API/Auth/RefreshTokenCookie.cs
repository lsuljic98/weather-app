using WeatherApp.Application.Auth;

namespace WeatherApp.API.Auth;

/// <summary>
/// The refresh token travels only as an HttpOnly cookie scoped to /api/auth, so it is never
/// readable by scripts and never attached to ordinary API calls.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "rt";
    public const string Path = "/api/auth";

    public static void Set(HttpResponse response, AuthResult result, AuthOptions options) =>
        response.Cookies.Append(Name, result.RefreshToken, Build(options, result.RefreshTokenExpiresAt));

    public static void Clear(HttpResponse response, AuthOptions options) =>
        response.Cookies.Delete(Name, Build(options, expires: null));

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) ? value : null;

    private static CookieOptions Build(AuthOptions options, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = options.SecureCookie,
        SameSite = SameSiteMode.Lax,
        Path = Path,
        Expires = expires,
        IsEssential = true,
    };
}
